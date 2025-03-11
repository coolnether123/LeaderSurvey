let questionCount = 0;

// Initialize form validation
document.addEventListener('DOMContentLoaded', function() {
    // Prevent form submission if no questions
    document.getElementById('surveyForm').addEventListener('submit', function(e) {
        if (questionCount === 0) {
            e.preventDefault();
            showNotification('Please add at least one question to the survey.', 'warning');
            return false;
        }
    });
});

function addQuestion() {
    if (questionCount >= 10) {
        showNotification('Maximum 10 questions allowed', 'warning');
        return;
    }

    questionCount++;
    const questionItem = createQuestionElement(questionCount);
    document.getElementById('questionsContainer').appendChild(questionItem);
    updateQuestionCounter();
}

function createQuestionElement(number) {
    const div = document.createElement('div');
    div.className = 'question-item d-flex align-items-center mb-3';
    div.innerHTML = `
        <i class="bi bi-grip-vertical question-drag-handle"></i>
        <div class="question-number">${number}</div>
        <div class="flex-grow-1">
            <input type="text" 
                   name="Survey.Questions[${number-1}].Text" 
                   class="form-control mb-2" 
                   placeholder="Enter your question here..." 
                   required>
            <select name="Survey.Questions[${number-1}].QuestionType" 
                    class="form-select question-type-select">
                <option value="yesno">Yes/No</option>
                <option value="score">Score (0-10)</option>
            </select>
        </div>
        <button class="cfa-btn cfa-btn-sm cfa-btn-outline ms-3" onclick="deleteQuestion(this)">
            <i class="bi bi-trash"></i>
        </button>
    `;

    // Add animation class
    div.classList.add('question-item-new');
    setTimeout(() => div.classList.remove('question-item-new'), 300);

    return div;
}

function deleteQuestion(button) {
    if (confirm('Are you sure you want to delete this question?')) {
        const questionItem = button.closest('.question-item');
        questionItem.classList.add('deleting');
        
        setTimeout(() => {
            questionItem.remove();
            questionCount--;
            updateQuestionCounter();
            renumberQuestions();
        }, 300);
    }
}

function updateQuestionCounter() {
    const counter = document.getElementById('question-counter');
    counter.textContent = `${questionCount}/10`;
    
    // Update add question button visibility
    const addButton = document.querySelector('#add-question-btn');
    if (addButton) {
        addButton.style.display = questionCount >= 10 ? 'none' : 'block';
    }
}

function renumberQuestions() {
    const questions = document.querySelectorAll('.question-item');
    questions.forEach((question, index) => {
        const number = index + 1;
        question.querySelector('.question-number').textContent = number;
        
        // Update input names to maintain proper indexing
        const textInput = question.querySelector('input[type="text"]');
        const typeSelect = question.querySelector('select');
        
        textInput.name = `Survey.Questions[${index}].Text`;
        typeSelect.name = `Survey.Questions[${index}].QuestionType`;
    });
}
// Add some CSS animations
const style = document.createElement('style');
style.textContent = `
    .question-item {
        transition: all 0.3s ease;
        opacity: 1;
        transform: translateX(0);
    }
    
    .question-item-new {
        opacity: 0;
        transform: translateX(-20px);
    }
    
    .deleting {
        opacity: 0;
        transform: translateX(20px);
    }
`;
document.head.appendChild(style);
