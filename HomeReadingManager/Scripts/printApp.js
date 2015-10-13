var printApp;
printApp = (function (app) {
    $('#printTable').click(function () {
        alert("jquery working")
        printApp.print();
    });
    $('#sendArray').click(function () {
        printApp.sendArray();
    });

    app.print = function () {
       
        $.ajax({
            url: 'Reports/Print',
            success: function (data) {
                if (printApp.arePopupsBlocked()) {
                    alert('Please allow popups.');
                }
                var printWindow = window.open();
                if (printWindow) {
                    $(printWindow.document.body).html(data);

                } else {
                    alert('Please allow popups.');
                }
            },
            error: function (xhr, textStatus, errorThrown) {
                if (xhr.status != 0) {
                    alert('Error 1');
                }
            }
        });
    };

    app.arePopupsBlocked = function () {
        var aWindow = window.open(null, "", "width=1,height=1");
        try {
            aWindow.close();
            return false;
        } catch (e) {
            return true;
        }
    };

    app.sendArray = function() {
        var payload = [];
        payload.push('1');
        payload.push('2');
        $.ajax({
            url: 'Reports/ProcessArrayData',
            data: JSON.stringify({ numbers: payload }),
            type: 'post',
            datatype: 'json',
            contentType: 'application/json',
            success: function(data) {
                alert(data);
            },
            error: function (xhr, textStatus, errorThrown) {
                if (xhr.status != 0) {
                    alert('Error 2');
                }
            }
        });
    };


    app.initailizeProgressMessage = function(url) {
        var id = $('#idField').val();
        printApp.setupTimer(url, id, false);
    };

    app.setupTimer = function(url, id) {
        printApp.checkForProgress(id, url, function (result) {
            if (result) {
                $('.progressMessage').show();
            } else {
                $('.progressMessage').hide();
            }
            setTimeout(function() {
                printApp.setupTimer(url, id);
            }, 1000);
        });

    };

    app.checkForProgress = function (id, url, callbackFunction) {
        try {
            $.ajax({
                url: url,
                data: { id: id },
                type: 'get',
                datatype: 'json',
                success: function(data) {
                    callbackFunction(data);
                },
                error: function(xhr, textStatus, errorThrown) {
                    callbackFunction(false);
                    if (xhr.status != 0) {
                        alert('Error 3');
                    }
                },
                global: false // override to keep regular ajaxStart ajaxEnd from firing
            });
        } catch(e) {
            callbackFunction(false);
        }
    };
    
    return app;
})(window.printApp || {})

printApp.initailizeProgressMessage('Reports/ProgressCheck')