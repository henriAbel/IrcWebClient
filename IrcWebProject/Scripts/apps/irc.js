var channels = {};
// Master model for switching between channels views
var master = {
    main: ko.observable(null),
    chanList: ko.observable(null),
    allChanList: ko.observable(null)
}

var IrcMessage = function (from, message, type) {
    this.from = from;
    this.message = message;
    this.type = type;
}

var IrcUser = function(name, op, voice, away) {
    this.name = name;
    this.op = op;
    this.voice = voice;
    this.away = away;

    this.startPrivate = function () {
        irc.checkChannel(this.name, true);
    }
}

var IrcChannel = function (name, privateChannel) {
    var self = this;
    this.name = name;
    this.privateChannel = privateChannel !== undefined ? privateChannel : false;

    this.changeChannel = function (item, event) {
        var vm = channels[this.name];
        master.main(vm);
        sly.reload();
        sly.toEnd(true);
        $('.channel-selected').removeClass('channel-selected');
        var channelWrapper;
        if (event !== undefined) {
            channelWrapper = $(event.target).parents('li');
        } // event is udefined if user not clicked on channel name. Eg on new channel join 
        else {
            channelWrapper = irc.findChannelElement(this.name);
        }
        channelWrapper.removeClass('new-message').addClass('channel-selected');

        // Bind/rebind editable topic handlers
        $('.topic-wrapper .topic').inlineEdit({
            save: function (e, data) {
                irc.sendMessage('/topic ' + self.name + ' :' + data.value);
                vm.setTopic(data.value);
                return true;
            }
        });
    }

    this.closeChannel = function () {
        if (!self.privateChannel) {
            irc.sendMessage("/part " + this.name, null);
        }

        delete channels[this.name];
        channelModel.channels2.remove(this);
        // This is not good way to choose new channel
        if (master.main().channel == this.name) {
            $('.channel-list span').last().click();
        }
    }
}

var ContentViewModel = function (channel) {
    var self = this;
    this.messages = ko.observableArray([]);
    this.users = ko.observableArray([]);
    this.topic = ko.observable();
    this.channel = channel;

    this.setTopic = function (topic) {
        this.topic(topic);
    }

    this.addUser = function(user) {
        if (this.users.indexOf(user) < 0) {
            this.users.push(user);
        }
    }

    this.empty = function () {
        this.users.removeAll();
    }

    this.sortedUsers = ko.computed(function () {
        return self.users().sort(function (left, right) {
            if (!left.op && right.op) return 1;
            if (left.op && !right.op) return -1;
            if (left.voice && right.op) return 1;
            if (left.voice && !right.voice) return -1;
            if (right.voice && !left.voice) return 1;
            return left.name.toLowerCase() > right.name.toLowerCase();
                
            return 0;
        });
    });
}

var ChannelViewModel = function () {
    var self = this;
    this.channels2 = ko.observableArray([]);

    this.sortedChannels = ko.computed(function () {
        return self.channels2().sort(function (left, right) {
            return left.name == right.name ?
                 0 :
                 (left.name < right.name ? 1 : -1);
        });
    });
}

// Model where user can select channels to join, or create new one
var channelListModel = function () {
    var self = this;
    this.allChannels = ko.observableArray([]);
    this.joinChannel = function (clickedChannel) {
        irc.joinChannel(clickedChannel);
        self.allChannels.remove(clickedChannel);
    }

    this.addChannel = function (form) {
        var input = $(form).find('#new-channel');
        var inputValue = input.val();
        if (inputValue.substring(0, 1) != "#") {
            inputValue = "#" + inputValue;
        }
        irc.sendMessage("/join " + inputValue);
        irc.hideChannelChoosingWindow();
        input.val('');
    }
}
var channelModel = new ChannelViewModel();
var channelListViewModel = new channelListModel();

var ircChat = function () {
    var self = this;
    // Register signalR connection
    var chatHub = $.connection.ircConnectorHub;
    // Login inormation in JSON
    var login = JSON.parse(loginData);
    var sendButton = $('#message-send');
    var messageBox = $('#message-text');
    var allChanelClose = $('#all-channels .close');
    var onPage = true;
    var newMessages = false;
    var notificationSound = new Audio('/Content/notify.mp3');
    var lastNotificationTime;
    var soundEnabled = true;
    var originalTitle = document.title;
    master.chanList(channelModel);
    master.allChanList(channelListViewModel);
    ko.applyBindings(master);

    var onTimer = function () {
        if (originalTitle == document.title && newMessages)
            document.title = "New messages";
        else
            document.title = originalTitle;
    }

    // Bind focus/blur events
    window.onfocus = function () {
        onPage = true;
        lastNotificationTime = undefined;
        newMessages = false;
        onTimer();
    };

    window.onblur = function () {
        onPage = false;
    };

    $.connection.hub.logging = false;

    // When hub sends new message
    chatHub.client.received = function (message) {
        // Escape message
        message.message = escape(message.message);
        // Replace url with <a> tag
        message.message = Autolinker.link(message.message);
        var model = self.checkChannel(message.channel);
        if (message.channel != 'server' && message.type != 'notification') {
            var currentTime = new Date().getTime();
            if (soundEnabled && !onPage && (undefined === lastNotificationTime || currentTime - lastNotificationTime > 20)) {
                lastNotificationTime = currentTime;
                notificationSound.play();
            }

            if (!onPage && !newMessages) {
                newMessages = true;
            }
        
            if (master.main().channel != message.channel) {
                self.findChannelElement(message.channel).addClass('new-message')
            }
        }
        var m = new IrcMessage(message.user, message.message, message.type);
        model.messages.push(m);
        sly.reload();
        sly.toEnd();
    };

    // Updates current userlist
    chatHub.client.userList = function (list) {
        var cleared = [];
        for (var i = 0; i < list.length; i++) {
            // Get channel name from list
            var user = list[i];
            var channel = user.Channel;
            if (null != channel) {
                var channelCWModel = self.checkChannel(channel);
                // Add user to right channel userlist
                if (cleared.indexOf(channel) < 0) {
                    channelCWModel.empty();
                    cleared.push(channel);
                }

                var u = new IrcUser(user.Name, user.Op, user.Voice, user.Away);
                channelCWModel.addUser(u);
            }
        }
    }

    chatHub.client.onTopicChange = function (channelObject) {
        var model = self.checkChannel(channelObject.Name);
        model.setTopic(channelObject.Topic);
    }

    chatHub.client.channelList = function (channel) {
        master.allChanList().allChannels.removeAll();
        for (i = 0; i < channel.length; i++) {
            master.allChanList().allChannels.push(channel[i]);
        }
        $('#all-channels').removeClass('hidden').addClass('channel-list-show');
    }
   
    $.connection.hub.start().done(function () {
        // Send connection request
        chatHub.server.startIrc(login);
    });
    
    messageBox.keypress(function (e) {
        // Enter
        if (e.which == 13) {
            self.sendCurrentMessage();
        }
    });

    sendButton.click(function (e) {
        self.sendCurrentMessage();
    });

    allChanelClose.click(function () {
        self.hideChannelChoosingWindow();
    });

    this.hideChannelChoosingWindow = function () {
        $('#all-channels').removeClass('channel-list-show').addClass('hidden');
    }

    this.sendCurrentMessage = function () {
        var val = messageBox.val();
        if (val.length > 0) {
            this.sendMessage(val, master.main().channel, function (e) {
                messageBox.val(null);
                messageBox.focus();
            });
        }
    }

    this.sendMessage = function (value, channel, callback) {
        chatHub.server.send(value, channel).done(function(e) {
            if (callback !== undefined) {
                callback(e);
            }
        })
    }

    this.disconnect = function () {
        chatHub.server.doDisconnect();
    }

    // Check if channel exists, if not then create
    this.checkChannel = function (channel, privateChannel) {
        // Make new content view model for channel
        if (!channels.hasOwnProperty(channel)) {
            channels[channel] = new ContentViewModel(channel);
            var c = new IrcChannel(channel, privateChannel);
            channelModel.channels2.push(c);
            c.changeChannel();
        }
        return channels[channel];
    }

    this.requestChannelList = function () {
        chatHub.server.requestChannelList();
    }

    this.joinChannel = function (channel) {
        chatHub.server.send('/join ' + channel, null);
    }

    this.setSoundEnabled = function (enabled) {
        soundEnabled = enabled;
    }
    
    // Returns li element thats hold channel name
    this.findChannelElement = function (channel) {
        return $(".channel-list span:contains('" + channel + "')").parents('li');
    }

    var int = setInterval(onTimer, 3000);
};




