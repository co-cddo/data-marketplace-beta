
function removeResponse(event) {
  event.preventDefault();
  
  const removeButton = event.target;
  let containerId = removeButton.getAttribute("data-container");
  let container = document.getElementById(containerId);

  const parentResponseGroupElement = event.target.parentElement;

  container.removeChild(parentResponseGroupElement);

  // Now that we've removed the element, get the total number of buttons remaining.
  // If there is just one left then remove it, as we don't want to be able to remove the final response
  const allRemoveButtons = document.getElementsByClassName("remove-multiple-answer");
  if (allRemoveButtons.length == 1) {
    allRemoveButtons[0].remove();
  }
}

function addDuplicateField(event) {
    event.preventDefault();
    const containerId = event.target.getAttribute("data-container");
    const container = document.getElementById(containerId);

    const firstResponseElement = container.firstElementChild;
    const clonedResponseElement = firstResponseElement.cloneNode(true);

    const uniqueId = Date.now();

    const inputField = clonedResponseElement.querySelector("input[type='text']");
    const suggestionsList = clonedResponseElement.querySelector("ul");

    inputField.value = '';

    inputField.id = `${inputField.id}-${uniqueId}`;
    inputField.setAttribute("data-suggestions-id", `autocomplete-suggestions-${uniqueId}`);
    inputField.oninput = (event) => filterSuggestions(event, uniqueId);

    if (suggestionsList) {
        suggestionsList.id = `autocomplete-suggestions-${uniqueId}`;
    }

    container.appendChild(clonedResponseElement);

    attachRemoveResponseButton(clonedResponseElement);
}

function filterSuggestions(event, uniqueId) {
    const input = event.target;
    const query = input.value.toLowerCase();
    const suggestionsList = document.getElementById(`autocomplete-suggestions-${uniqueId}`);

    suggestionsList.innerHTML = '';

    if (query.length > 0) {
        const filteredCountries = countries.filter(country =>
            country.toLowerCase().includes(query)
        );

        filteredCountries.forEach(country => {
            const li = document.createElement('li');
            li.textContent = country;
            li.classList.add('autocomplete-suggestion');
            li.onclick = () => selectSuggestion(country, input, `autocomplete-suggestions-${uniqueId}`);
            suggestionsList.appendChild(li);
        });
    }
}

function selectSuggestion(country, input, suggestionsListId) {
    input.value = country;
    document.getElementById(suggestionsListId).innerHTML = '';
}

const addResponseButtons = document.querySelectorAll(".add-another-answer");
if (addResponseButtons.length > 0)
{
    addResponseButtons.forEach(function (button) {
        button.addEventListener("click", addDuplicateField);
    });
}

const removeResponseButtons = document.querySelectorAll(".remove-multiple-answer");
if (removeResponseButtons.length > 0) {
  removeResponseButtons.forEach(function (removeResponseButton) {
    removeResponseButton.addEventListener("click", removeResponse);
  });
}

const conditionalCheckboxes = document.querySelectorAll('.controlling-conditional');
if (conditionalCheckboxes && conditionalCheckboxes.length > 0) {
    conditionalCheckboxes.forEach(function (checkbox) {
        const inputId = checkbox.id + "-supplementary";
        const input = document.getElementById(inputId);
        if (checkbox.checked) {
            input.removeAttribute("disabled");
        }
        checkbox.addEventListener('change', function () {
            const inputId = checkbox.id + "-supplementary";
            const input = document.getElementById(inputId);
            if (checkbox.checked) {
                input.removeAttribute("disabled");
            } else {
                input.setAttribute("disabled", "disabled");
            }
        });
    });
}

document.getElementById('save-and-return').addEventListener('click', function () {
    document.getElementById('show-next-question').value = 'false';
    document.querySelector('form').submit();
});