import Utility from './utility.js';

const _utility = new Utility(null, document.getElementById('alert1'), true);
const urlAssistant = 'api1/v1.1/assistant';

let assistantId = null;
let threadId = null;

async function sendMessage(message) {
    //show placeholder spinner in the chat output
    const thinking = showChatThinking();
    toggleInputEnable(false);

    //send 
    const method = 'POST';
    const url = urlAssistant;
    const payload = { "AssistantId": assistantId, "ThreadId": threadId, "Message": message };

    try {
        const response = await _utility.HttpSend(method, url, payload);
        if (response.ok) {

            //get response
            const data = await response.data;

            //hold the chat Id
            assistantId = data.assistantId;
            threadId = data.threadId;

            appendMessage('system', data.message);
        }
    }
    finally {
        thinking.remove();
        toggleInputEnable(true);
    }
}

function showChatThinking() {
    const thinking = document.createElement('div');
    thinking.classList.add('message');
    const spin = document.getElementById('spinner-rings').cloneNode(true);
    spin.removeAttribute('hidden');
    thinking.appendChild(spin);
    let elChat = document.getElementById('chat-output');
    elChat.appendChild(thinking);
    elChat.scrollTop = elChat.scrollHeight;
    return thinking;
}

function appendMessage(source, message) {
    let msg = document.createElement('div');
    msg.classList.add('message', 'message-' +source);
    msg.innerHTML = message;
    let elChat = document.getElementById('chat-output');
    elChat.appendChild(msg);
    elChat.scrollTop = elChat.scrollHeight;
}

async function appendAndSend() {
    document.getElementById('alert1').innerHTML = "";
    const userMessage = document.getElementById('chat-input').value.trim();
    if (userMessage.length == 0) return;
    document.getElementById('chat-input').value = "";
    appendMessage('user', userMessage); 
    await sendMessage(userMessage);
}

async function newChat() {
    assistantId = null;
    threadId = null;
    document.getElementById('chat-output').innerHTML = "";
    document.getElementById('chat-input').value = "";
    await sendMessage("hi");
}

function toggleInputEnable(toggle) {
    if (!toggle) {
        document.getElementById('chat-controls').classList.add('disabled');
    }
    else {
        document.getElementById('chat-controls').classList.remove('disabled');
        document.getElementById('chat-input').focus();
    }
    document.getElementById('chat-input').placeholder = toggle ? "Type your message here..." : "Please wait...";
}

//wire up event handlers
document.getElementById('chat-send').addEventListener('click', appendAndSend);
document.getElementById('chat-new').addEventListener('click', newChat);
document.addEventListener('keydown', function (event) {
    if (event.key === 'Enter') {
        appendAndSend();
    }
});

//init
await newChat();
