﻿if (typeof (jQuery) != 'undefined') {
    $(function () {
        function youTubeDimension(attr, defaultSize) {
            var customDimension = parseInt($("[data-" + attr + "]").data(attr));
            if (customDimension) {
                return customDimension;
            }
            return defaultSize;
        }

        var youTube = new RegExp(/^https?:\/\/(youtu.be\/|www.youtube.com\/watch\?v=)([A-Za-z0-9_-]+)$/);
        var youTubeWidth = youTubeDimension("video-width", 450), youTubeHeight = youTubeDimension("video-height", 318), youTubeResize = false;

        function embedYouTube() {
            $("a.embed").filter(function (index) {
                return youTube.test(this.href);
            }).each(function () {

                if ($(this.parentNode).width() >= youTubeWidth) {

                    // Swop YouTube link for embedded video
                    var match = youTube.exec(this.href);
                    $(this).replaceWith('<iframe width="' + youTubeWidth + '" height="' + youTubeHeight + '" src="https://www.youtube-nocookie.com/embed/' + match[2] + '?rel=0" frameborder="0" allowfullscreen="allowfullscreen" class="video"></iframe>');

                    // Once the window's big enough, stop watching resize
                    if (youTubeResize) {
                        $(window).off("resize", embedYouTube);
                        youTubeResize = false;
                    }
                } else {
                    // If the window isn't big enough, watch for resize
                    if (!youTubeResize) {
                        $(window).on("resize", embedYouTube);
                        youTubeResize = true;
                    }
                }
            });
        };

        embedYouTube();
    });
}
