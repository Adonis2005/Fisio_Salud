// FisioSalud - Scripts generales
$(function () {
    // Las vistas usan nombres de atributos nuevos; el runtime local es Bootstrap 4.
    // Normalizarlos permite usar su API oficial sin descargar dependencias externas.
    $('[data-bs-toggle], [data-bs-dismiss], [data-bs-target]').each(function () {
        var element = this;
        ['toggle', 'dismiss', 'target', 'parent', 'backdrop', 'keyboard'].forEach(function (name) {
            var value = element.getAttribute('data-bs-' + name);
            if (value !== null) element.setAttribute('data-' + name, value);
        });
    });
    $('.btn-close').addClass('close').attr('aria-label', 'Cerrar').each(function () {
        if (!this.textContent.trim()) this.textContent = '\u00d7';
    });
    // Un modal dentro de una tabla puede quedar recortado o invalidar su estructura.
    $('.modal').appendTo(document.body);
    $('#toggleSidebar').on('click', function () {
        $('#sidebar').toggleClass('show');
    });

    $('#mobileMenuBtn').on('click', function () {
        $('#mobileMenuPanel').toggleClass('hidden');
    });
});
