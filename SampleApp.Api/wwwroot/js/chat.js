import Utility from './utility.js';

const _utility = new Utility(document.querySelector(".spinner"), document.querySelector("#message"), true);
const urlChat = 'api1/v1.1/chat';

async function sendMessage() {
    const input = document.getElementById('chat-input').value.trim();

    //send 
    const method = "POST";
    const url = urlChat;
    
    const response = await _utility.HttpSend(method, url, input);
    if (response.ok) {

        //append user message to the chat output
        appendMessage('user', input);

        //get response
        const data = await response.json();

        //append system message to the chat output
        appendMessage('system', data.message);

        //clear input
        document.getElementById('chat-input').innerText = "";
    }
}

function appendMessage(source, message) {
    let msg = document.createElement('div');
    msg.className = source;
    msg.innerText = message;
    document.getElementById('chat-output').appendChild(msg);
}

function clearEditRow() {
    popEdit("", false, "", "", "")
    document.getElementById('btn-save').textContent = 'Add';
}

//wire up event handlers
document.getElementById('send').addEventListener('click', sendMessage);
