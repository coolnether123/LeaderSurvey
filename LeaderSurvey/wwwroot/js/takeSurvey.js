/**
 * Leader Survey - Take Survey JavaScript
 * This file contains the client-side functionality for the TakeSurvey page.
 */

// Function to initialize the survey form
function initializeSurveyForm() {
    const form = document.getElementById('surveyForm');
    if (!form) return;

    // Add form submission handler
    form.addEventListener('submit', validateSurveyForm);

    // Initialize score buttons
    initializeScoreButtons();
}

// Function to validate the survey form before submission
function validateSurveyForm(event) {
    event.preventDefault();

    let isValid = true;

    // Validate leader selection
    const leaderSelect = document.querySelector('select[name="SelectedLeaderId"]');
    if (!leaderSelect.value) {
        isValid = false;
        leaderSelect.classList.add('is-invalid');
        const errorElement = leaderSelect.parentElement.nextElementSibling;
        if (errorElement && errorElement.classList.contains('text-danger')) {
            errorElement.textContent = 'Please select a leader to evaluate.';
        }
    } else {
        leaderSelect.classList.remove('is-invalid');
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
            if (yesRadio.checked) {
                answer = 'Yes';
            } else if (noRadio.checked) {
                answer = 'No';
            }
        } else if (questionType === 'score') {
            const checkedInput = document.querySelector(`input[name="Answers[${questionId}]"]:checked`);
            answer = checkedInput ? checkedInput.value : '';
        } else if (questionType === 'text') {
            const textarea = card.querySelector('textarea');
            answer = textarea.value.trim();
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
        showLoadingIndicator();
        form.submit();
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
    }
}

// Function to handle the cancel button
function cancelSurvey() {
    if (confirm('Are you sure you want to cancel? Any answers you have entered will be lost.')) {
        window.location.href = '/Surveys';
    }
}
