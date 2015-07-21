﻿// Code from http://code.google.com/p/analytics-api-samples/source/browse/trunk/src/tracking/javascript/v5/social/ga_social_tracking.js
// but with unused bits commented out

// Copyright 2011 Google Inc. All Rights Reserved.

/**
* @fileoverview A simple script to automatically track Facebook 
* buttons using Google Analytics social tracking feature.
* @author api.nickm@google.com (Nick Mihailovski)
*/


/**
* Namespace.
* @type {Object}.
*/
var _ga = _ga || {};


/**
* Ensure global _gaq Google Analytics queue has been initialized.
* @type {Array}
*/
var _gaq = _gaq || [];


/**
* Helper method to track social features. This assumes all the social
* scripts / apis are loaded synchronously. If they are loaded async,
* you might need to add the nextwork specific tracking call to the
* a callback once the network's script has loaded.
* @param {string} opt_pageUrl An optional URL to associate the social
*     tracking with a particular page.
* @param {string} opt_trackerName An optional name for the tracker object.
*/
/*_ga.trackSocial = function (opt_pageUrl, opt_trackerName)
{
    _ga.trackFacebook(opt_pageUrl, opt_trackerName);
};*/


/**
* Tracks Facebook likes, unlikes and sends by suscribing to the Facebook
* JSAPI event model. Note: This will not track facebook buttons using the
* iFrame method.
* @param {string} opt_pageUrl An optional URL to associate the social
*     tracking with a particular page.
* @param {string} opt_trackerName An optional name for the tracker object.
*/
_ga.trackFacebook = function (opt_pageUrl, opt_trackerName)
{
    var trackerName = _ga.buildTrackerName_(opt_trackerName);
    try
    {
        if (FB && FB.Event && FB.Event.subscribe)
        {
            FB.Event.subscribe('edge.create', function (targetUrl)
            {
                _gaq.push([trackerName + '_trackSocial', 'facebook', 'like',
            targetUrl, opt_pageUrl]);
            });
            FB.Event.subscribe('edge.remove', function (targetUrl)
            {
                _gaq.push([trackerName + '_trackSocial', 'facebook', 'unlike',
            targetUrl, opt_pageUrl]);
            });
            FB.Event.subscribe('message.send', function (targetUrl)
            {
                _gaq.push([trackerName + '_trackSocial', 'facebook', 'send',
            targetUrl, opt_pageUrl]);
            });
        }
    } catch (e) { }
};


/**
* Returns the normalized tracker name configuration parameter.
* @param {string} opt_trackerName An optional name for the tracker object.
* @return {string} If opt_trackerName is set, then the value appended with
*     a . Otherwise an empty string.
* @private
*/
_ga.buildTrackerName_ = function (opt_trackerName)
{
    return opt_trackerName ? opt_trackerName + '.' : '';
};


/**
* Extracts a query parameter value from a URI.
* @param {string} uri The URI from which to extract the parameter.
* @param {string} paramName The name of the query paramater to extract.
* @return {string} The un-encoded value of the query paramater. underfined
*     if there is no URI parameter.
* @private
*/
_ga.extractParamFromUri_ = function (uri, paramName)
{
    if (!uri)
    {
        return;
    }
    var uri = uri.split('#')[0];  // Remove anchor.
    var parts = uri.split('?');  // Check for query params.
    if (parts.length == 1)
    {
        return;
    }
    var query = decodeURI(parts[1]);

    // Find url param.
    paramName += '=';
    var params = query.split('&');
    for (var i = 0, param; param = params[i]; ++i)
    {
        if (param.indexOf(paramName) === 0)
        {
            return unescape(param.split('=')[1]);
        }
    }
    return;
};


/* Wire up the Facebook 'Like' button asynchronously including Google Analytics social tracking 
   Code from http://analytics-api-samples.googlecode.com/svn/trunk/src/tracking/javascript/v5/social/facebook_js_async.html
   but using our Facebook app_id 
*/
(function ()
{
    // Don't load in IE6 to avoid "facebook.com is opening trusted site eastsussex.gov.uk" on County Council PCs.
    if (typeof (jQuery) != 'undefined' && $.browser.msie && parseInt($.browser.version, 10) == 6) return;

    var fb = document.getElementById('fb-root');
    if (!fb) return;

    var e = document.createElement('script'); e.async = true;
    e.src = document.location.protocol + '//connect.facebook.net/en_US/all.js';
    fb.appendChild(e);
}());

window.fbAsyncInit = function ()
{
    FB.init({ appId: '169406409819518', status: true, cookie: true,
        xfbml: true
    });

    _ga.trackFacebook();
};