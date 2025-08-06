/**
 * Leader Survey - Take Survey JavaScript
 * This file contains the client-side functionality for the TakeSurvey page.
 */

// DEBUG: Check if this file is loaded - REMOVE AFTER DEBUGGING
console.log('takeSurvey.js loaded at', new Date().toISOString());

// DEBUG: Define global functions for testing - REMOVE AFTER DEBUGGING
window.takeSurveyJsLoaded = true;

// Function to initialize the survey form
function initializeSurveyForm() {
    console.log('initializeSurveyForm called at', new Date().toISOString());

    const form = document.getElementById('surveyForm');
    if (!form) {
        console.error('Survey form not found during initialization');
        return;
    }

    console.log('Initializing survey form...');

    // Initialize score buttons
    initializeScoreButtons();

    // Add submit event listener to the form
    form.addEventListener('submit', validateSurveyForm);
    console.log('Added submit event listener to form');

    // Display any validation errors from server-side
    const errorMessage = document.getElementById('formErrorMessage');
    if (errorMessage && errorMessage.textContent) {
        errorMessage.classList.add('alert', 'alert-danger');
    }

    console.log('Survey form initialization complete');
}

// Function to validate the survey form before submission
/**
 * Validates the survey form and submits it via AJAX if valid.
 * This function prevents the default form submission.
 * @param {Event} event - The form submission event.
 */
function validateSurveyForm(event) {
    // Prevent the browser's default behavior of a full-page form submission.
    event.preventDefault();

    // --- 1. SETUP & INITIALIZATION ---
    let isValid = true;
    const form = document.getElementById('surveyForm');

    // Clear any previous error messages before starting new validation.
    clearValidationErrors();

    // --- 2. VALIDATION LOGIC ---

    // a) Validate that a leader has been selected (if the dropdown exists).
    const leaderSelect = document.querySelector('select[name="SelectedLeaderId"]');
    if (leaderSelect && !leaderSelect.value) {
        isValid = false;
        leaderSelect.classList.add('is-invalid');
        const errorElement = leaderSelect.parentElement?.nextElementSibling;
        if (errorElement) {
            errorElement.textContent = 'Please select a leader to evaluate.';
        }
    }

    // b) Validate that every question has been answered.
    const questionCards = document.querySelectorAll('.question-card');
    questionCards.forEach(card => {
        const questionId = card.dataset.questionId;
        const questionType = card.dataset.questionType;
        let isAnswered = false;

        // Check for an answer based on the question's type.
        if (questionType === 'yesno' || questionType === 'score') {
            // For radio buttons, an answer exists if one is checked.
            if (form.querySelector(`input[name="Answers[${questionId}]"]:checked`)) {
                isAnswered = true;
            }
        } else if (questionType === 'text') {
            // For text areas, an answer exists if the text is not just whitespace.
            const textarea = card.querySelector('textarea');
            if (textarea && textarea.value.trim() !== '') {
                isAnswered = true;
            }
        }

        // If a question is not answered, mark it as invalid and show an error.
        if (!isAnswered) {
            isValid = false;
            const errorElement = document.getElementById(`error_${questionId}`);
            if (errorElement) {
                errorElement.textContent = 'Please provide an answer for this question.';
                errorElement.style.display = 'block';
                // Add a red border to the card to make the error more visible.
                card.classList.add('border-danger');
            }
        } else {
            // If it is answered, ensure no error state is shown.
            card.classList.remove('border-danger');
        }
    });

    // --- 3. AJAX SUBMISSION ---

    // Only proceed with submission if the entire form is valid.
    if (isValid) {
        showLoadingIndicator();

        // Use FormData to easily collect all form inputs, including answers.
        const formData = new FormData(form);

        // ===================================================================
        // THE CORE FIX: Get the Anti-Forgery Token from the hidden form field.
        // ASP.NET Core requires this token to be sent with POST requests to prevent
        // Cross-Site Request Forgery (CSRF) attacks.
        // ===================================================================
        const token = document.querySelector('input[name="__RequestVerificationToken"]').value;

        // Submit the form data using the JavaScript Fetch API.
        fetch(form.action, {
            method: 'POST',
            body: formData,
            headers: {
                // Add the anti-forgery token to the request headers.
                'RequestVerificationToken': token,
                // These headers tell the server this is an AJAX request and we expect a JSON response.
                'X-Requested-With': 'XMLHttpRequest',
                'Accept': 'application/json'
            }
        })
            .then(async response => {
                let data;
                try {
                    // Try to parse the response as JSON.
                    data = await response.json();
                } catch (e) {
                    // If the server returns something other than JSON (like an error page), handle it gracefully.
                    throw new Error('Server returned an unexpected response. Please try again.');
                }

                // If the response status is not OK (e.g., 400, 500) or the JSON indicates failure...
                if (!response.ok || (data && data.success === false)) {
                    if (data && data.errors) {
                        // Display specific validation errors returned from the server.
                        displayValidationErrors(data.errors);
                    } else {
                        // Display a generic error message from the server.
                        showGenericErrorMessage(data.message || 'An error occurred. Please try again.');
                    }
                    hideLoadingIndicator();
                } else {
                    // Success! The survey was submitted correctly.
                    console.log('Survey submitted successfully:', data);
                    showSuccessBanner(data.message || 'Survey saved successfully. Thank you!');
                    disableForm(); // Make the form read-only to prevent re-submission.
                    hideLoadingIndicator();
                }
            })
            .catch(error => {
                // This catches network errors or errors from the .then() block.
                console.error('Error submitting survey:', error);
                hideLoadingIndicator();
                showGenericErrorMessage(error.message);
            });
    } else {
        // If validation fails, show a general error message and scroll to the first error.
        showGenericErrorMessage('Please correct the highlighted errors before submitting.');
        const firstError = document.querySelector('.is-invalid, .border-danger');
        if (firstError) {
            firstError.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    }
}

// Function to initialize score buttons
function initializeScoreButtons() {
    // Set initial values for all score displays
    document.querySelectorAll('[id^="scoreValue_"]').forEach(display => {
        const questionId = display.id.replace('scoreValue_', '');
        const checkedInput = document.querySelector(`input[name="Answers[${questionId}]"]:checked`);
        if (checkedInput) {
            updateScoreValue(questionId, checkedInput.value);
        }
    });
}

// Function to update the score value display
function updateScoreValue(questionId, value) {
    const valueDisplay = document.getElementById(`scoreValue_${questionId}`);
    if (valueDisplay) {
        valueDisplay.textContent = value;

        // Update the color based on the value
        const numValue = parseInt(value);
        valueDisplay.className = 'badge px-3 py-2';

        if (numValue >= 8 && numValue <= 10) {
            valueDisplay.classList.add('bg-success'); // Green for 8-10
        } else if (numValue >= 5 && numValue <= 7) {
            valueDisplay.classList.add('bg-warning'); // Yellow for 5-7
        } else if (numValue >= 1 && numValue <= 4) {
            valueDisplay.classList.add('bg-danger'); // Red for 1-4
        } else {
            // For 0 or invalid values, use a neutral color
            valueDisplay.classList.add('bg-secondary');
        }
    }
}

// Function to show a loading indicator when submitting the form
function showLoadingIndicator() {
    const submitButton = document.getElementById('submitSurvey');
    if (submitButton) {
        submitButton.disabled = true;
        submitButton.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Submitting...';
        console.log('Loading indicator shown, button disabled');
    } else {
        console.error('Submit button not found!');
    }
}

// Function to hide the loading indicator
function hideLoadingIndicator() {
    const submitButton = document.getElementById('submitSurvey');
    if (submitButton) {
        submitButton.disabled = false;
        submitButton.innerHTML = '<i class="bi bi-send"></i> Submit Survey';
        console.log('Loading indicator hidden, button enabled');
    }
}

// Function to show a success banner
function showSuccessBanner(message) {
    // Check if there's already a banner
    let banner = document.getElementById('successBanner');

    // If not, create one
    if (!banner) {
        banner = document.createElement('div');
        banner.id = 'successBanner';
        banner.className = 'alert alert-success alert-dismissible fade show success-banner';
        banner.role = 'alert';
        banner.innerHTML = `
            <i class="bi bi-check-circle-fill me-2"></i> <span id="successMessage"></span>
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        `;

        // Add the banner to the top of the page
        const container = document.querySelector('.container') || document.body;
        container.insertBefore(banner, container.firstChild);

        // Add CSS for the banner if it doesn't exist
        if (!document.getElementById('successBannerStyle')) {
            const style = document.createElement('style');
            style.id = 'successBannerStyle';
            style.textContent = `
                .success-banner {
                    position: fixed;
                    top: 20px;
                    left: 50%;
                    transform: translateX(-50%);
                    z-index: 9999;
                    min-width: 300px;
                    max-width: 80%;
                    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
                    animation: slideDown 0.5s ease-out;
                }
                @keyframes slideDown {
                    from { top: -100px; opacity: 0; }
                    to { top: 20px; opacity: 1; }
                }
            `;
            document.head.appendChild(style);
        }
    }

    // Set the message
    const messageElement = banner.querySelector('#successMessage');
    if (messageElement) {
        messageElement.textContent = message;
    }

    // Auto-dismiss after 5 seconds
    setTimeout(() => {
        if (banner && banner.parentNode) {
            banner.classList.remove('show');
            setTimeout(() => {
                if (banner && banner.parentNode) {
                    banner.parentNode.removeChild(banner);
                }
            }, 500);
        }
    }, 5000);
}

// Function to disable the form after successful submission
function disableForm() {
    const form = document.getElementById('surveyForm');
    if (form) {
        // Disable all form elements
        const formElements = form.querySelectorAll('input, textarea, select, button');
        formElements.forEach(element => {
            element.disabled = true;
        });

        // Add a visual indication that the form is disabled
        form.classList.add('form-disabled');

        // Add a message at the top of the form
        const formMessage = document.createElement('div');
        formMessage.className = 'alert alert-info mt-3';
        formMessage.innerHTML = '<i class="bi bi-info-circle me-2"></i> This survey has been submitted. Thank you for your feedback!';

        // Insert the message at the top of the form
        form.insertBefore(formMessage, form.firstChild);

        // Add a button to go back to surveys
        const backButton = document.createElement('a');
        backButton.href = '/Surveys';
        backButton.className = 'cfa-btn cfa-btn-primary mt-3';
        backButton.innerHTML = '<i class="bi bi-arrow-left"></i> Back to Surveys';

        // Add the button after the form
        form.parentNode.insertBefore(backButton, form.nextSibling);

        console.log('Form disabled after successful submission');
    }
}

// Function to handle the cancel button
function cancelSurvey() {
    if (confirm('Are you sure you want to cancel? Any answers you have entered will be lost.')) {
        window.location.href = '/Surveys';
    }
}

// Function to display validation errors
function displayValidationErrors(errors) {
    console.log('Displaying validation errors:', errors);

    // Clear previous error messages
    clearValidationErrors();

    // Get the form error message container
    const formErrorMessage = document.getElementById('formErrorMessage');
    if (formErrorMessage) {
        formErrorMessage.innerHTML = '<div class="alert alert-danger"><i class="bi bi-exclamation-triangle-fill me-2"></i><strong>Please correct the following errors:</strong><ul class="mt-2 mb-0" id="validationErrorsList"></ul></div>';

        const errorsList = document.getElementById('validationErrorsList');

        // Add each error to the list
        let hasErrors = false;
        for (const [key, messages] of Object.entries(errors)) {
            console.log('Processing error for key:', key, 'messages:', messages);
            if (messages && messages.length > 0) {
                hasErrors = true;

                // Try to find the related form element
                let fieldName = key;
                if (key.startsWith('Answers[')) {
                    // Extract the question ID from the key
                    const questionId = key.match(/\[(\d+)\]/)?.[1];
                    console.log('Found question ID:', questionId);
                    if (questionId) {
                        // Find the question card
                        const questionCard = document.querySelector(`.question-card[data-question-id="${questionId}"]`);
                        console.log('Found question card:', questionCard);
                        if (questionCard) {
                            // Get the question text
                            const questionText = questionCard.querySelector('strong')?.textContent;
                            if (questionText) {
                                fieldName = questionText;
                            }

                            // Mark the question as invalid
                            const inputElement = questionCard.querySelector('input, textarea');
                            if (inputElement) {
                                inputElement.classList.add('is-invalid');
                            }

                            // Show the error message in the question card
                            const errorElement = document.getElementById(`error_${questionId}`);
                            if (errorElement) {
                                errorElement.textContent = messages[0];
                                errorElement.style.display = 'block';
                            }
                        } else {
                            console.warn('Question card not found for ID:', questionId);
                        }
                    }
                } else if (key === 'SelectedLeaderId') {
                    fieldName = 'Leader to Evaluate';

                    // Mark the select as invalid
                    const leaderSelect = document.querySelector('select[name="SelectedLeaderId"]');
                    if (leaderSelect) {
                        leaderSelect.classList.add('is-invalid');

                        // Show the error message
                        const errorElement = leaderSelect.parentElement?.nextElementSibling;
                        if (errorElement && errorElement.classList.contains('text-danger')) {
                            errorElement.textContent = messages[0];
                        }
                    }
                }

                // Add the error to the list
                messages.forEach(message => {
                    const li = document.createElement('li');
                    li.innerHTML = `<strong>${fieldName}:</strong> ${message}`;
                    errorsList.appendChild(li);
                });
            }
        }

        // If there are no specific field errors, but we have a general error
        if (!hasErrors && errors[''] && errors[''].length > 0) {
            const li = document.createElement('li');
            li.textContent = errors[''][0];
            errorsList.appendChild(li);
        }

        // Scroll to the error message
        formErrorMessage.scrollIntoView({ behavior: 'smooth', block: 'center' });
    } else {
        console.error('formErrorMessage element not found');
    }
}

// Function to clear validation errors
function clearValidationErrors() {
    // Clear the form error message
    const formErrorMessage = document.getElementById('formErrorMessage');
    if (formErrorMessage) {
        formErrorMessage.innerHTML = '';
    }

    // Clear all field-specific error messages
    document.querySelectorAll('.is-invalid').forEach(element => {
        element.classList.remove('is-invalid');
    });

    // Clear all error messages in question cards
    document.querySelectorAll('[id^="error_"]').forEach(element => {
        element.textContent = '';
        element.style.display = 'none';
    });
}

// Function to show a generic error message
function showGenericErrorMessage(message) {
    const formErrorMessage = document.getElementById('formErrorMessage');
    if (formErrorMessage) {
        formErrorMessage.innerHTML = `<div class="alert alert-danger"><i class="bi bi-exclamation-triangle-fill me-2"></i><strong>Error:</strong> ${message || 'An error occurred while submitting the form. Please try again.'}</div>`;
        formErrorMessage.scrollIntoView({ behavior: 'smooth', block: 'center' });
    } else {
        console.error('formErrorMessage element not found');
    }
}

// Simple function to show loading indicator when form is submitted
function showSubmitLoading() {
    const form = document.getElementById('surveyForm');
    if (form) {
        form.addEventListener('submit', function() {
            const submitButton = document.getElementById('submitSurvey');
            if (submitButton) {
                submitButton.disabled = true;
                submitButton.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Submitting...';
                console.log('Form is being submitted, showing loading indicator');
            }
        });
    }
}

// Call this function when the page loads
document.addEventListener('DOMContentLoaded', function() {
    showSubmitLoading();
});
