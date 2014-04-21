var irc;
var sly;

$(function () {
    var t;
    // Dynamic content height
    $('#frame').css('height', $(window).height() - 90) + 'px';
    $(window).resize(function () {
        clearTimeout(t);
        t = setTimeout(function () {
            $('#frame').css('height', $(window).height() - 90) + 'px';
            sly.reload();
        }, 100);
    });

    // High perfomance scroll component
    sly = new Sly('#frame', {
        speed: 0,
        scrollBy: 60,
        dragHandle: 1,
        dynamicHandle: 1,
        clickBar: 1,
        touchDragging: 1,
        scrollBar: '#scrollbar',
    }).init();

    // Irc application itself
    irc = new ircChat();
    

    // Log out button binding
    $('.log-out').click(function () {
        irc.disconnect();
    });

    $('#channel-list').click(function () {
        irc.requestChannelList();
    });

    $('#message-text').focus();
});

var chars = {
    "<": "&lt;",
    ">": "&gt;",
    "/": '&#x2F;',
    '"': '&quot;',
    "'": '&#39;',
    "&": "&amp;"
};

function escape(string) {
    return String(string).replace(/[&<>"'\/]/g, function (s) {
        return chars[s];
    });
}