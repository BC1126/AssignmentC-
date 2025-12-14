$(document).ready(function () {
    function setupPreview(inputId, previewId) {
        $('#' + inputId).on('change', function () {
            const file = this.files[0];
            if (!file) return;

            const reader = new FileReader();
            reader.onload = function (e) {
                $('#' + previewId).attr('src', e.target.result).show();
            };
            reader.readAsDataURL(file);
        });
    }

    setupPreview('posterInput', 'posterPreview');
    setupPreview('bannerInput', 'bannerPreview');
});
