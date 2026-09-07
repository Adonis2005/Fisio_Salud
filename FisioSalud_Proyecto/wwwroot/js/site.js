// FisioSalud - Scripts generales
$(function () {
    $('#toggleSidebar').on('click', function () {
        $('#sidebar').toggleClass('show');
    });

    $('#mobileMenuBtn').on('click', function () {
        $('#mobileMenuPanel').toggleClass('hidden');
    });
});