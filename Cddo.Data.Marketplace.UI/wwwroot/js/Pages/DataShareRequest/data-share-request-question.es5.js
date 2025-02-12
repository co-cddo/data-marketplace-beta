"use strict";

function addDuplicateField(event) {
    event.preventDefault();
    const containerId = event.target.getAttribute("data-container");

    const container = document.getElementById(containerId);
    const firstChild = container.firstElementChild;
    if (firstChild) {
        const clonedElement = firstChild.cloneNode(true);
        container.appendChild(clonedElement);

        const removeButton = document.createElement("button");
        removeButton.textContent = "Remove";
        removeButton.classList.add("govuk-button", "govuk-button--secondary");
        removeButton.addEventListener("click", function () {
            container.removeChild(clonedElement);
            container.removeChild(removeButton);
            if (container.children.length === 1) {
                container.removeChild(container.querySelector('.remove-button'));
            }
        });
        container.appendChild(removeButton);
    }
}

const addButtons = document.querySelectorAll(".add-another-answer");
addButtons.forEach(function (button) {
    button.addEventListener("click", addDuplicateField);
});

document.getElementById('save-and-return').addEventListener('click', function () {
    document.getElementById('show-next-question').value = 'false';
    document.querySelector('form').submit();
});
