/*
 * jQuery inlineEdit
 *
 * Copyright (c) 2009 Ca-Phun Ung <caphun at yelotofu dot com>
 * Licensed under the MIT (MIT-LICENSE.txt) license.
 *
 * http://github.com/caphun/jquery.inlineedit/
 *
 * Inline (in-place) editing.
 */

(function ($) {

    $.fn.inlineEdit = function (options) {

        // define some options with sensible default values
        // - hoverClass: the css classname for the hover style
        // - save: a callback triggered on save
        options = $.extend({
            hover: 'hover',
            save: ''
        }, options);

        return $.each(this, function () {

            // define self container
            var self = $(this);

            // create a value property to keep track of current value
            self.value = self.text();
            self.doBlur = true;

            self.keypress(function (e) {
                if (e.which == 13) {
                    self.doSubmit(e);
                }
            });

            // bind the click event to the current element, in this example it's span.editable
            self.bind('click', function (event) {

                // for event delegation
                var $this = $(event.target);

                // check if click  was applied to the save button
                if ($this.is('button')) {
                    self.doSubmit(event);
                } else if ($this.is(self[0].tagName)) {

                    self
                        // populate current element with an input element
                        // and add the current value to it
                        .html('<input type="text" value="' + self.value + '"> <button>Save</button>')
                        // select this newly created input element
                        .find('input')
                            // bind the blur event and make it save back the value to the original span area 
                            // there by replacing our dynamically generated input element
                            .bind('blur', function () {
                                // check if timer is set and clear it if so
                                if (self.timer) {
                                    window.clearTimeout(self.timer);
                                }
                                // set timer so that blur doesn't immediately convert the input into the
                                // non-editable format
                                self.timer = window.setTimeout(function () {
                                    if (self.doBlur) {
                                        self.text(self.value);
                                        self.removeClass(options.hover);
                                    }
                                }, 200);
                            })
                            // give the newly created input element focus
                            .focus();

                }
            })
            // on hover add hoverClass, on rollout remove hoverClass
            .hover(
                function () {
                    $(this).addClass(options.hover);
                },
                function () {
                    $(this).removeClass(options.hover);
                }
            );

            self.doSubmit = function (event) {
                var $this = $(event.target);
                // create a hash for our callback
                var hash = {
                    value: $input = $this.siblings('input').val()
                };
                if (hash.value === undefined)
                    hash.value = $this.val();

                // check if callback function set and execute it, save only if callback does not returns false
                if (($.isFunction(options.save) && options.save.call(self, event, hash)) !== false || !options.save) {
                    self.doBlur = false;
                }
            }
        });
    }

})(jQuery);