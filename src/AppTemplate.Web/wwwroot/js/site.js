// Site-wide client standards. Every page gets these through _Layout.cshtml.
(function ($) {
    'use strict';

    const app = window.app = window.app || {};

    // --- Antiforgery: every AJAX request sends the token; the server rejects POSTs without it.
    $.ajaxSetup({
        headers: { 'RequestVerificationToken': $('meta[name="request-verification-token"]').attr('content') }
    });

    // --- Dates: the format comes from the server (DateFormat.DatePickerFormat), never typed here.
    app.datePickerFormat = $('meta[name="app-date-format"]').attr('content');

    app.initDatePickers = function (root) {
        $(root || document).find('input[data-datepicker]').datepicker({
            dateFormat: app.datePickerFormat,
            changeMonth: true,
            changeYear: true
        });
    };

    // --- Errors: one handler for every failed AJAX call. The server returns ProblemDetails
    // with a user-safe title and a traceId that matches the logs.
    app.showError = function (message) {
        const alert = $('<div class="alert alert-danger alert-dismissible fade show" role="alert"></div>')
            .text(message)
            .append('<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>');
        $('#app-alerts').append(alert);
    };

    $(document).ajaxError(function (event, xhr) {
        if (xhr.statusText === 'abort') {
            return;
        }
        const problem = xhr.responseJSON;
        const title = problem && problem.title ? problem.title : 'Something went wrong. Please try again.';
        app.showError(problem && problem.traceId ? title + ' Reference: ' + problem.traceId : title);
    });

    // --- DataTables: server-side, JSON body, text-escaped cells, errors through ajaxError.
    $.fn.dataTable.ext.errMode = 'none';

    app.dataTable = function (selector, url, columns, options) {
        // Escape cell text unless a column supplies its own renderer. Without this, a customer
        // named <script>... would run in every user's browser (stored XSS).
        const safeColumns = columns.map(function (c) {
            return c.render ? c : $.extend({ render: $.fn.dataTable.render.text() }, c);
        });
        return $(selector).DataTable($.extend(true, {
            serverSide: true,
            processing: true,
            searchDelay: 400,
            pageLength: 25,
            lengthMenu: [10, 25, 50, 100],
            ajax: {
                url: url,
                type: 'POST',
                contentType: 'application/json',
                data: function (d) { return JSON.stringify(d); }
            },
            columns: safeColumns
        }, options));
    };

    $(function () {
        app.initDatePickers();
    });
})(jQuery);
