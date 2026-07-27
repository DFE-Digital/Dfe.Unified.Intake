// Client-side virus-scan orchestration for the Check your answers page.
//
// Progressive enhancement: with JavaScript off the form posts straight to the server, which scans the
// files synchronously before submitting. With JavaScript on we intercept the submit, ask the server to
// start an asynchronous ClamAV scan for each uploaded file, then poll each job and show a colour-coded
// per-file status above the button. The button stays disabled throughout; only once every file reports
// "clean" do we write the confirmed job ids into a hidden field and let the form submit for real.
(function () {
    "use strict";

    var panel = document.getElementById("virus-scan-status");
    if (!panel) {
        return; // No supporting files on this page — nothing to scan.
    }

    var form = panel.closest("form");
    var rows = Array.prototype.slice.call(panel.querySelectorAll("[data-stored-name]"));
    if (!form || rows.length === 0) {
        return;
    }

    var button = form.querySelector('[data-prevent-double-click="true"]') ||
        form.querySelector("button.govuk-button");
    var jobIdsInput = form.querySelector("#ScanJobIds");
    var tokenInput = form.querySelector('input[name="__RequestVerificationToken"]');

    var pollInterval = (parseInt(panel.getAttribute("data-poll-interval"), 10) || 5) * 1000;
    var maxAttempts = parseInt(panel.getAttribute("data-max-attempts"), 10) || 60;
    var startUrl = panel.getAttribute("data-start-url");
    var statusUrl = panel.getAttribute("data-status-url");

    var started = false;
    var attempts = 0;

    // GOV.UK tag styling per scan status. Pending states share one "scanning" look.
    function tagFor(status) {
        switch (status) {
            case "clean": return { text: "Clean", modifier: "govuk-tag--green" };
            case "infected": return { text: "Failed check", modifier: "govuk-tag--red" };
            case "error": return { text: "Could not check", modifier: "govuk-tag--orange" };
            default: return { text: "Scanning", modifier: "govuk-tag--blue" };
        }
    }

    function setTag(row, status) {
        var tag = row.querySelector("[data-scan-tag]");
        if (!tag) {
            return;
        }
        var look = tagFor(status);
        tag.className = "govuk-tag " + look.modifier;
        tag.textContent = look.text;
    }

    function rowFor(storedName) {
        for (var i = 0; i < rows.length; i++) {
            if (rows[i].getAttribute("data-stored-name") === storedName) {
                return rows[i];
            }
        }
        return null;
    }

    function disableButton() {
        if (button) {
            button.setAttribute("disabled", "disabled");
            button.setAttribute("aria-disabled", "true");
        }
    }

    function enableButton() {
        if (button) {
            button.removeAttribute("disabled");
            button.removeAttribute("aria-disabled");
        }
    }

    function showMessage(text) {
        var existing = document.getElementById("virus-scan-message");
        if (!existing) {
            existing = document.createElement("p");
            existing.id = "virus-scan-message";
            existing.className = "govuk-error-message";
            panel.appendChild(existing);
        }
        existing.textContent = text;
    }

    // A terminal failure the user cannot retry away (a file is infected). Leave the button disabled so
    // they go back and remove the offending file.
    function infected() {
        showMessage("One or more of your files failed our virus check. Go back and remove the affected " +
            "file before submitting.");
    }

    // A transient failure (API error or timeout). Re-enable the button so the user can try again.
    function failed(text) {
        showMessage(text);
        started = false;
        enableButton();
    }

    function succeed() {
        var ids = rows
            .map(function (row) { return row.getAttribute("data-job-id"); })
            .filter(function (id) { return !!id; });

        if (jobIdsInput) {
            jobIdsInput.value = ids.join(",");
        }
        // Mark the form verified and submit programmatically. form.submit() does not fire submit
        // listeners, so this does not re-enter the handler below.
        form.setAttribute("data-scan-verified", "true");
        form.submit();
    }

    function fetchStatus(row) {
        var jobId = row.getAttribute("data-job-id");
        if (!jobId) {
            return Promise.resolve("error");
        }
        return fetch(statusUrl + "&jobId=" + encodeURIComponent(jobId), {
            headers: { "Accept": "application/json" }
        })
            .then(function (response) {
                if (!response.ok) {
                    throw new Error("status " + response.status);
                }
                return response.json();
            })
            .then(function (data) {
                setTag(row, data.status);
                return data.status;
            })
            .catch(function () {
                setTag(row, "error");
                return "error";
            });
    }

    function pollRound() {
        attempts += 1;
        Promise.all(rows.map(fetchStatus)).then(function (statuses) {
            if (statuses.indexOf("infected") !== -1) {
                infected();
                return;
            }
            if (statuses.indexOf("error") !== -1) {
                failed("We could not complete the virus check. Please try again.");
                return;
            }
            if (statuses.every(function (s) { return s === "clean"; })) {
                succeed();
                return;
            }
            if (attempts >= maxAttempts) {
                failed("The virus check is taking longer than expected. Please try again.");
                return;
            }
            window.setTimeout(pollRound, pollInterval);
        });
    }

    function startScan() {
        disableButton();
        panel.hidden = false;
        rows.forEach(function (row) { setTag(row, "scanning"); });

        var headers = { "Accept": "application/json" };
        if (tokenInput) {
            headers["RequestVerificationToken"] = tokenInput.value;
        }

        fetch(startUrl, { method: "POST", headers: headers })
            .then(function (response) {
                if (!response.ok) {
                    throw new Error("start failed " + response.status);
                }
                return response.json();
            })
            .then(function (data) {
                (data.files || []).forEach(function (file) {
                    var row = rowFor(file.storedName);
                    if (row) {
                        row.setAttribute("data-job-id", file.jobId);
                    }
                });
                attempts = 0;
                pollRound();
            })
            .catch(function () {
                failed("We could not start the virus check. Please try again.");
            });
    }

    form.addEventListener("submit", function (event) {
        // Already verified by a prior scan — let the real submission through.
        if (form.getAttribute("data-scan-verified") === "true") {
            return;
        }
        event.preventDefault();
        if (started) {
            return;
        }
        started = true;
        startScan();
    });
})();
