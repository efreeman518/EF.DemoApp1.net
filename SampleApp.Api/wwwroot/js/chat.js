import Utility from './utility.js';

const _utility = new Utility(null, document.getElementById("alert1"), true);
const urlChat = 'api1/v1.1/chat';

let chatId = null;

async function sendMessage(message) {
    //show placeholder spinner in the chat output
    const thinking = document.createElement("div");
    thinking.classList.add("message");
    const spin = document.getElementById("spinner-rings").cloneNode(true);
    //spin.classList.add("spinner");
    spin.removeAttribute("hidden");
    thinking.appendChild(spin);
    document.getElementById('chat-output').appendChild(thinking);

    //send 
    const method = "POST";
    const url = urlChat;
    const payload = { "ChatId": chatId, "Message": message };

    try {
        const response = await _utility.HttpSend(method, url, payload);
        if (response.ok) {

            //get response
            const data = await response.data;

            //hold the chat Id
            chatId = data.chatId;

            thinking.remove();
            appendMessage('system', data.message);
        }
    }
    finally {
        thinking.remove();
    }
}

function appendMessage(source, message) {
    let msg = document.createElement('div');
    msg.classList.add("message", "message-" +source);
    msg.innerHTML = message;
    let elChat = document.getElementById('chat-output');
    elChat.appendChild(msg);
    elChat.scrollTop = elChat.scrollHeight;
}

async function appendAndSend() {
    document.getElementById('alert1').innerHTML = "";
    const userMessage = document.getElementById('chat-input').value.trim();
    document.getElementById("chat-input").value = "";
    appendMessage('user', userMessage); 
    await sendMessage(userMessage);
}

async function newChat() {
    chatId = null;
    document.getElementById('chat-output').innerHTML = "";
    document.getElementById("chat-input").value = "";
    await sendMessage("hi");
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
await sendMessage("hi");
