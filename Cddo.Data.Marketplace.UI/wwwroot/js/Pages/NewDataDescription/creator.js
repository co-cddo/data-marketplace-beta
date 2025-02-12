let creatorIndex = 0;
let isFirstField = true; // Track if it's the first field

function addCreatorField() {
    creatorIndex++;
    const fieldset = document.getElementById('creatorFields');
    const lastFormGroup = fieldset.lastElementChild;

    const newFormGroup = lastFormGroup.cloneNode(true);

    const newSelect = newFormGroup.querySelector('.govuk-select');
    newSelect.id = `metadataCreator[${creatorIndex}]`;
    newSelect.name = `metadataCreator[${creatorIndex}]`;

    let removeBtn = newFormGroup.querySelector('.remove-creator');
    if (!removeBtn) {
        const removeButton = document.createElement('button');
        removeButton.type = 'button';
        removeButton.className = 'govuk-button govuk-button--secondary remove-creator mt15';
        removeButton.textContent = 'Remove';
        newFormGroup.appendChild(removeButton);
        removeBtn = removeButton;
    }

    if (isFirstField) {
        // If it's the first field, we add a remove button
        const firstFormGroup = fieldset.firstElementChild;
        const firstRemoveBtn = firstFormGroup.querySelector('.remove-creator');
        if (!firstRemoveBtn) {
            const firstRemoveButton = document.createElement('button');
            firstRemoveButton.type = 'button';
            firstRemoveButton.className = 'govuk-button govuk-button--secondary remove-creator mt15';
            firstRemoveButton.textContent = 'Remove';
            firstFormGroup.appendChild(firstRemoveButton);
            // Add event listener to the remove button of the first field
            firstRemoveButton.addEventListener('click', function () {
                fieldset.removeChild(firstFormGroup);
                creatorIndex--;

                // Update IDs and names of remaining form elements
                const remainingFormGroups = fieldset.querySelectorAll('.govuk-form-group');
                remainingFormGroups.forEach((formGroup, index) => {
                    const select = formGroup.querySelector('.govuk-select');
                    if (select) {
                        select.id = `metadataCreator[${index}]`;
                        select.name = `metadataCreator[${index}]`;
                    }
                });
                if (fieldset.childElementCount === 1) {
                    // If there's only one field left, remove the remove button from the first field
                    const firstFormGroup = fieldset.firstElementChild;
                    const firstRemoveBtn = firstFormGroup.querySelector('.remove-creator');
                    if (firstRemoveBtn) {
                        firstFormGroup.removeChild(firstRemoveBtn);
                    }
                    isFirstField = true; // Update flag
                }
            });
        }
        isFirstField = false; // Update flag
    }

    removeBtn.addEventListener('click', function () {
        fieldset.removeChild(newFormGroup);
        creatorIndex--;

        // Update IDs and names of remaining form elements
        const remainingFormGroups = fieldset.querySelectorAll('.govuk-form-group');
        remainingFormGroups.forEach((formGroup, index) => {
            const select = formGroup.querySelector('.govuk-select');
            if (select) {
                select.id = `metadataCreator[${index}]`;
                select.name = `metadataCreator[${index}]`;
            }
        });
        if (fieldset.childElementCount === 1) {
            // If there's only one field left, remove the remove button from the first field
            const firstFormGroup = fieldset.firstElementChild;
            const firstRemoveBtn = firstFormGroup.querySelector('.remove-creator');
            if (firstRemoveBtn) {
                firstFormGroup.removeChild(firstRemoveBtn);
            }
            isFirstField = true; // Update flag
        }
    });
    fieldset.appendChild(newFormGroup);
}