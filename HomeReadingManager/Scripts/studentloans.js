//studentloans.js

$(function () {
    $('#dvPager').on('click', 'a', function () {
        $.ajax({
            url: this.href,
            type: 'GET',
            cache: false,
            success: function (result) {
                           
                $('#dvLoans').html(result);
            }
        });
        return false;
    });
});


function BookSearch(studentId, search, page) {
    $.ajax({
        type: 'POST',
        url: '@Url.Content("~/Students/BookSearch")',
        data: {
        studentId: parseInt(studentId),
        search: search,
        page: page
        },
    dataType: 'html',
    success: function (result) {
        $('#dvLoans').empty().append(result);
        $('#scanArea').hide();
        // $("#modal-content").empty().append(result);
    }
});
}

$(function () {
    $('.selectTitle').on('click', function (evt) {
        evt.preventDefault();
        evt.stopPropagation();
        var id = $(this).attr('data-Id');
        var studentId = $('#selectedId').val();
        $.ajax({
            type: 'POST',
            url: '@Url.Content("~/Students/BookSelection")',
            data: {
            studentId: studentId,
            productId: id
            },
        dataType: 'json',
        success: function (result) {
            $('#scanArea').show();
            LoadCurrentLoans(studentId, result.Result)
        }
    });
    //    error: function (result) {
    //    $('#scanArea').Show();
    //    LoadCurrentLoans(studentId, "There was an error process this book. Please try again.")
    //    }
    //});
});
});

function LoadCurrentLoans(studentId, message) {

    $.ajax({
        type: 'POST',
        url: '@Url.Content("~/Students/ShowCurrentLoansView")',
        data: {
        studentId: parseInt(studentId)
        },
    dataType: 'html',
    success: function (result) {
        $('#dvMessages').html(message);
        $('#dvLoans').empty().append(result);
    }
});
}