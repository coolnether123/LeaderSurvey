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
    // DEBUG: Added more detailed logging - REMOVE AFTER DEBUGGING
    console.log('initializeSurveyForm called at', new Date().toISOString());

    const form = document.getElementById('surveyForm');
    if (!form) {
        console.error('Survey form not found during initialization');
        return;
    }

    console.log('Initializing survey form...');

    // Initialize score buttons
    initializeScoreButtons();

    // Add event listener to submit button
    const submitButton = document.getElementById('submitSurvey');
    if (submitButton) {
        // DEBUG: Log that we found the button - REMOVE AFTER DEBUGGING
        console.log('Submit button found, adding event listener');

        // Remove any existing event listeners
        submitButton.replaceWith(submitButton.cloneNode(true));

        // Get the fresh reference after replacement
        const freshButton = document.getElementById('submitSurvey');
        if (freshButton) {
            freshButton.addEventListener('click', function(event) {
                // DEBUG: Log button click - REMOVE AFTER DEBUGGING
                console.log('Submit button clicked via event listener');
                if (typeof window.submitSurveyForm === 'function') {
                    window.submitSurveyForm();
                } else {
                    console.error('submitSurveyForm function not found on window object');
                    // Fallback to direct form submission
                    validateAndSubmitForm();
                }
            });
        } else {
            console.error('Failed to get fresh reference to submit button');
        }
    } else {
        console.error('Submit button not found during initialization');
    }

    // Display any validation errors from server-side
    const errorMessage = document.getElementById('formErrorMessage');
    if (errorMessage && errorMessage.textContent) {
        errorMessage.classList.add('alert', 'alert-danger');
    }

    // DEBUG: Log initialization complete - REMOVE AFTER DEBUGGING
    console.log('Survey form initialization complete');
}

// Function to validate the survey form before submission
function validateSurveyForm(event) {
    event.preventDefault();

    let isValid = true;

    // Validate leader selection
    const leaderSelect = document.querySelector('select[name="SelectedLeaderId"]');
    // Only validate if the select element exists (it might not if leader is pre-selected)
    if (leaderSelect) {
        if (!leaderSelect.value) {
            isValid = false;
            leaderSelect.classList.add('is-invalid');
            const errorElement = leaderSelect.parentElement?.nextElementSibling;
            if (errorElement && errorElement.classList.contains('text-danger')) {
                errorElement.textContent = 'Please select a leader to evaluate.';
            }
        } else {
            leaderSelect.classList.remove('is-invalid');
        }
    }

    // Validate all questions have answers
    const questionCards = document.querySelectorAll('.question-card');
    questionCards.forEach(card => {
        const questionId = card.dataset.questionId;
        const questionType = card.dataset.questionType;
        let answer = '';

        // Get the answer based on question type
        if (questionType === 'yesno') {
            const yesRadio = document.getElementById(`yes_${questionId}`);
            const noRadio = document.getElementById(`no_${questionId}`);
            if (yesRadio && yesRadio.checked) {
                answer = 'Yes';
            } else if (noRadio && noRadio.checked) {
                answer = 'No';
            }
        } else if (questionType === 'score') {
            const checkedInput = document.querySelector(`input[name="Answers[${questionId}]"]:checked`);
            answer = checkedInput ? checkedInput.value : '';
        } else if (questionType === 'text') {
            const textarea = card.querySelector('textarea');
            if (textarea) {
                answer = textarea.value.trim();
            }
        }

        // Validate the answer
        const errorElement = document.getElementById(`error_${questionId}`);
        if (!answer) {
            isValid = false;
            const inputElement = card.querySelector('input, textarea');
            if (inputElement) {
                inputElement.classList.add('is-invalid');
            }
            if (errorElement) {
                errorElement.textContent = 'Please provide an answer for this question.';
                errorElement.style.display = 'block';
            }
        } else {
            const inputElement = card.querySelector('input, textarea');
            if (inputElement) {
                inputElement.classList.remove('is-invalid');
            }
            if (errorElement) {
                errorElement.textContent = '';
                errorElement.style.display = 'none';
            }
        }
    });

    // If the form is valid, submit it
    if (isValid) {
        try {
            showLoadingIndicator();
            const form = document.getElementById('surveyForm');
            if (form) {
                console.log('Submitting survey form...');
                // Add a small delay to ensure the UI updates before submission
                setTimeout(() => {
                    try {
                        form.submit();
                    } catch (submitError) {
                        console.error('Error submitting form:', submitError);
                        const errorMsg = document.getElementById('formErrorMessage');
                        if (errorMsg) {
                            errorMsg.textContent = 'Error submitting form: ' + submitError.message;
                            errorMsg.classList.add('alert', 'alert-danger');
                        }
                    }
                }, 100);
            } else {
                console.error('Survey form not found!');
            }
        } catch (error) {
            console.error('Error in form submission process:', error);
        }
    } else {
        // Scroll to the first error
        const firstError = document.querySelector('.is-invalid');
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

        if (numValue >= 8) {
            valueDisplay.classList.add('bg-success');
        } else if (numValue >= 4) {
            valueDisplay.classList.add('bg-primary');
        } else {
            valueDisplay.classList.add('bg-danger');
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

// Function to handle the cancel button
function cancelSurvey() {
    if (confirm('Are you sure you want to cancel? Any answers you have entered will be lost.')) {
        window.location.href = '/Surveys';
    }
}

// Function to submit the survey form - make it globally accessible
// DEBUG: Explicitly attach to window object - REMOVE AFTER DEBUGGING
window.submitSurveyForm = function() {
    console.log('Submit button clicked - DEBUG: From window.submitSurveyForm - REMOVE AFTER DEBUGGING');
    validateAndSubmitForm();
};

// Also define as regular function for backward compatibility
function submitSurveyForm() {
    console.log('Submit button clicked - DEBUG: From function submitSurveyForm - REMOVE AFTER DEBUGGING');
    window.submitSurveyForm();
}

// Function to validate and submit the form
function validateAndSubmitForm() {
    let isValid = true;
    const form = document.getElementById('surveyForm');
    if (!form) {
        console.error('Survey form not found!');
        return;
    }

    // Validate leader selection
    const leaderSelect = document.querySelector('select[name="SelectedLeaderId"]');
    // Only validate if the select element exists (it might not if leader is pre-selected)
    if (leaderSelect) {
        if (!leaderSelect.value) {
            isValid = false;
            leaderSelect.classList.add('is-invalid');
            const errorElement = leaderSelect.parentElement?.nextElementSibling;
            if (errorElement && errorElement.classList.contains('text-danger')) {
                errorElement.textContent = 'Please select a leader to evaluate.';
            }
        } else {
            leaderSelect.classList.remove('is-invalid');
        }
    }

    // Validate all questions have answers
    const questionCards = document.querySelectorAll('.question-card');
    questionCards.forEach(card => {
        const questionId = card.dataset.questionId;
        const questionType = card.dataset.questionType;
        let answer = '';

        // Get the answer based on question type
        if (questionType === 'yesno') {
            const yesRadio = document.getElementById(`yes_${questionId}`);
            const noRadio = document.getElementById(`no_${questionId}`);
            if (yesRadio && yesRadio.checked) {
                answer = 'Yes';
            } else if (noRadio && noRadio.checked) {
                answer = 'No';
            }
        } else if (questionType === 'score') {
            const checkedInput = document.querySelector(`input[name="Answers[${questionId}]"]:checked`);
            answer = checkedInput ? checkedInput.value : '';
        } else if (questionType === 'text') {
            const textarea = card.querySelector('textarea');
            if (textarea) {
                answer = textarea.value.trim();
            }
        }

        // Validate the answer
        const errorElement = document.getElementById(`error_${questionId}`);
        if (!answer) {
            isValid = false;
            const inputElement = card.querySelector('input, textarea');
            if (inputElement) {
                inputElement.classList.add('is-invalid');
            }
            if (errorElement) {
                errorElement.textContent = 'Please provide an answer for this question.';
                errorElement.style.display = 'block';
            }
        } else {
            const inputElement = card.querySelector('input, textarea');
            if (inputElement) {
                inputElement.classList.remove('is-invalid');
            }
            if (errorElement) {
                errorElement.textContent = '';
                errorElement.style.display = 'none';
            }
        }
    });

    // If the form is valid, submit it
    if (isValid) {
        try {
            showLoadingIndicator();
            console.log('Form is valid, submitting...');
            form.submit();
        } catch (error) {
            console.error('Error in form submission:', error);
            const errorMsg = document.getElementById('formErrorMessage');
            if (errorMsg) {
                errorMsg.textContent = 'Error submitting form: ' + error.message;
                errorMsg.classList.add('alert', 'alert-danger');
            }
        }
    } else {
        console.log('Form validation failed');
        // Scroll to the first error
        const firstError = document.querySelector('.is-invalid');
        if (firstError) {
            firstError.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }

        // Show general error message
        const errorMsg = document.getElementById('formErrorMessage');
        if (errorMsg) {
            errorMsg.textContent = 'Please correct the errors above before submitting.';
            errorMsg.classList.add('alert', 'alert-danger');
        }
    }
}
