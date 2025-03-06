document.addEventListener('DOMContentLoaded', function () {
    const formContainer = document.getElementById('format-addition');
    formContainer.addEventListener('click', function (event) {
        if (event.target.classList.contains('remove-input')) {
            const inputGroup = event.target.closest('.govuk-form-group');
            if (inputGroup) {
                inputGroup.remove();
            }
        }
    });

    const addButton = document.getElementById('add-another-format');
    addButton.addEventListener('click', function () {
        const inputCount = formContainer.querySelectorAll('.govuk-input').length;
        const newInput = document.createElement('div');
        newInput.className = 'govuk-form-group';
        newInput.innerHTML = `
            <label class="govuk-label govuk-visually-hidden" for="format-${inputCount}">formats</label>
            <div class="govuk-input__wrapper">
                <input class="govuk-input govuk-input--width-10" id="format-${inputCount}" name="format" type="text">
                <button type="button" class="remove-input govuk-button govuk-button--secondary mb-0">Remove</button>  
            </div>
        `;
        formContainer.appendChild(newInput);
    });
});