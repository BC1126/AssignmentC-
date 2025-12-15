// Initiate GET request (AJAX-supported)
$(document).on('click', '[data-get]', e => {
    e.preventDefault();
    const url = e.target.dataset.get;
    location = url || location;
});

// Initiate POST request (AJAX-supported)
$(document).on('click', '[data-post]', e => {
    e.preventDefault();
    const url = e.target.dataset.post;
    const f = $('<form>').appendTo(document.body)[0];
    f.method = 'post';
    f.action = url || location;
    f.submit();
});

// Trim input
$('[data-trim]').on('change', e => {
    e.target.value = e.target.value.trim();
});

// Auto uppercase
$('[data-upper]').on('input', e => {
    const a = e.target.selectionStart;
    const b = e.target.selectionEnd;
    e.target.value = e.target.value.toUpperCase();
    e.target.setSelectionRange(a, b);
});

// RESET form
$('[type=reset]').on('click', e => {
    e.preventDefault();
    location = location;
});

$(document).ready(function () {
    function setupPreview(inputId, previewId, existingId) {
        $('#' + inputId).on('change', function () {
            const file = this.files[0];
            if (!file) return;

            // hide existing image
            $('#' + existingId).hide();

            const reader = new FileReader();
            reader.onload = function (e) {
                $('#' + previewId)
                    .attr('src', e.target.result)
                    .show();
            };
            reader.readAsDataURL(file);
        });
    }

    setupPreview('posterInput', 'posterPreview', 'existingPoster');
    setupPreview('bannerInput', 'bannerPreview', 'existingBanner');
});


// Check all checkboxes
$('[data-check]').on('click', e => {
    e.preventDefault();
    const name = e.target.dataset.check;
    $(`[name=${name}]`).prop('checked', true);
});

// Uncheck all checkboxes
$('[data-uncheck]').on('click', e => {
    e.preventDefault();
    const name = e.target.dataset.uncheck;
    $(`[name=${name}]`).prop('checked', false);
});

// Row checkable (AJAX-supported)
$(document).on('click', '[data-checkable]', e => {
    if ($(e.target).is(':input,a')) return;
    
    $(e.currentTarget)
        .find(':checkbox')
        .prop('checked', (i, v) => !v);
});

// Photo preview
$('.upload input').on('change', e => {
    const f = e.target.files[0];
    const img = $(e.target).siblings('img')[0];

    img.dataset.src ??= img.src;

    if (f && f.type.startsWith('image/')) {
        img.onload = e => URL.revokeObjectURL(img.src);
        img.src = URL.createObjectURL(f);
    }
    else {
        img.src = img.dataset.src;
        e.target.value = '';
    }

    // Trigger input validation
    $(e.target).valid();
});

//Payment method
$('[data-method]').on('click', e => {
    const method = e.target.dataset.method;

    if (method === "creditCard") {
        document.getElementById("payWithCard").style.display = "block";
        document.getElementById("payWithEwallet").style.display = "none";
    } else {
        document.getElementById("payWithCard").style.display = "none";
        document.getElementById("payWithEwallet").style.display = "block";
    }
});

$(document.getElementById('promotionBtn')).on('click', e => {
    const promo = document.getElementById('promotionList');

    if (promo.style.display = "none")
        promo.style.display = "block";
    else
        promo.style.display = "none";
});