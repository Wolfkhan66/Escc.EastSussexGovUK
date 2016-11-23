Param(
  [Parameter(HelpMessage="Where are the source files for the application? (required)")]
  [string]$sourceFolder,
	
  [Parameter(Mandatory=$True,HelpMessage="Where is the folder where applications are installed to? (required)")]
  [string]$destinationFolder,
  
  [Parameter(HelpMessage="Where is the folder where applications are backed up before being updated?")]
  [string]$backupFolder,
	
  [Parameter(Mandatory=$True,HelpMessage="Where are the XDT transforms for the *.example.config files? (required)")]
  [string]$transformsFolder,

  [Parameter(Mandatory=$True,HelpMessage="What is the name of the IIS site where the application is being set up? (required)")]
  [string]$websiteName,

  [Parameter(Mandatory=$True,HelpMessage="Why is this change being made? (required)")]
  [string]$comment
)

########################################################################
### BOOTSTRAP. Copy this section into each application setup script  ###
###            to make sure required functions are available.        ###
########################################################################

$pathOfThisScript = Split-Path $MyInvocation.MyCommand.Path -Parent
$parentFolderOfThisScript = $pathOfThisScript | Split-Path -Parent
$scriptsProject = 'Escc.WebApplicationSetupScripts'
$functionsPath = "$pathOfThisScript\..\$scriptsProject\functions.ps1"
if (Test-Path $functionsPath) {
  Write-Host "Checking $scriptsProject is up-to-date"
  Push-Location "$pathOfThisScript\..\$scriptsProject"
  git pull origin master
  Pop-Location
  Write-Host
  .$functionsPath
} else {
  if ($env:GIT_ORIGIN_URL) {
    $repoUrl = $env:GIT_ORIGIN_URL -f $scriptsProject
    git clone $repoUrl "$pathOfThisScript\..\$scriptsProject"
  } 
  else 
  {
    Write-Warning '$scriptsProject project not found. Please set a GIT_ORIGIN_URL environment variable on your system so that it can be downloaded.
  
Example: C:\>set GIT_ORIGIN_URL=https://example-git-server.com/{0}"
  
{0} will be replaced with the name of the repository to download.'
    Exit
  }
}

########################################################################
### END BOOTSTRAP. #####################################################
########################################################################

# Copy a minimal set of files needed to load the ESCC website template on a site which will request most of its components using the remote template.

$projectName = "Escc.EastSussexGovUK.TemplateSource" 
$sourceFolder = NormaliseFolderPath $sourceFolder "$PSScriptRoot\$projectName"
$destinationFolder = NormaliseFolderPath $destinationFolder
$backupFolder = NormaliseFolderPath $backupFolder
if (!$backupFolder) { $backupFolder = "$destinationFolder\backups" }
$backupFolder = "$backupFolder\$websiteName"
$destinationFolder = "$destinationFolder\$websiteName"
$transformsFolder = NormaliseFolderPath $transformsFolder

BackupApplication "$destinationFolder\$projectName" $backupFolder $comment
    
robocopy $sourceFolder "$destinationFolder\$projectName" /S /PURGE /IF *.dll *.ico *.css *.js apple-*.png navigation.png desktop.png pan-*.gif item-type.png default.aspx logo-large.gif google*.html /XD aspnet_client obj Properties "Web References"
TransformConfig "$sourceFolder\web.example.config" "$destinationFolder\$projectName\web.temp1.config" "$PSScriptRoot\Escc.EastSussexGovUK.WebForms.NuGet\web.config.install.xdt"
TransformConfig "$destinationFolder\$projectName\web.temp1.config" "$destinationFolder\$projectName\web.temp2.config" "$PSScriptRoot\Escc.EastSussexGovUK.SecurityConfig.NuGet\web.config.install.xdt"
TransformConfig "$destinationFolder\$projectName\web.temp2.config" "$destinationFolder\$projectName\web.temp3.config" "$PSScriptRoot\Escc.EastSussexGovUK.Metadata.NuGet\web.config.install.xdt"
TransformConfig "$destinationFolder\$projectName\web.temp3.config" "$destinationFolder\$projectName\web.config" "$transformsFolder\$projectName\web.release.config"
del "$destinationFolder\$projectName\web.temp*.config"

TransformConfig "$sourceFolder\css\web.example.config" "$destinationFolder\$projectName\css\web.config" "$transformsFolder\$projectName\css\web.release.config"
TransformConfig "$sourceFolder\js\web.example.config" "$destinationFolder\$projectName\js\web.config" "$transformsFolder\$projectName\js\web.release.config"

CheckSiteExistsBeforeAddingApplication $websiteName
Write-Host "Setting IIS site root folder to $destinationFolder\$projectName" 
Set-ItemProperty "IIS:\Sites\$websiteName" -Name PhysicalPath -Value "$destinationFolder\$projectName"
 
Write-Host
Write-Host "Done." -ForegroundColor "Green"