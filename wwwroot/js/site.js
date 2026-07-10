// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Prevents a form being submitted more than once. GOV.UK Frontend already debounces rapid double
// clicks on buttons marked data-prevent-double-click; this goes further and disables the button for
// the entire request, which matters when submitting is slow. Progressive enhancement: with JavaScript
// off the form still works, and a failed submission simply re-renders the page with the button
// enabled again.
(function () {
    "use strict";

    var SELECTOR = '[data-prevent-double-click="true"]';

    // Disable the submit button once its form starts submitting. Capture phase so we run before the
    // navigation begins; the timeout defers the disable until after the browser has captured the form
    // data, so nothing that depends on the button being enabled at submit time is affected.
    document.addEventListener("submit", function (event) {
        var button = event.target.querySelector(SELECTOR);
        if (!button || button.getAttribute("aria-disabled") === "true") {
            return;
        }

        window.setTimeout(function () {
            button.setAttribute("disabled", "disabled");
            button.setAttribute("aria-disabled", "true");
        }, 0);
    }, true);

    // If the page is restored from the browser's back/forward cache the disabled button would still be
    // disabled, stranding the user. Re-enable it when that happens.
    window.addEventListener("pageshow", function (event) {
        if (!event.persisted) {
            return;
        }

        var buttons = document.querySelectorAll(SELECTOR);
        for (var i = 0; i < buttons.length; i++) {
            buttons[i].removeAttribute("disabled");
            buttons[i].removeAttribute("aria-disabled");
        }
    });
})();
