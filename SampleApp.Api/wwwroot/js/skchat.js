import Utility from './utility.js';
//import { BlandWebClient } from './external/BlandClient.js';

//https://github.com/CINTELLILABS/bland-client-js-sdk/blob/main/dist/lib/es6/client/BlandClient.js

const _utility = new Utility(null, document.getElementById('alert1'), true);
const urlChat = 'api1/v1.1/skchat';
const urlBland = 'api1/v1.1/blandai';

let chatId = null;

async function sendMessage(message) {
    //show placeholder spinner in the chat output
    const thinking = _utility.showSpinner(document.getElementById('chat-output'), 'message', true);
    toggleInputEnable(false);

    //send 
    const method = 'POST';
    const url = urlChat;
    const payload = { "ChatId": chatId, "Message": message };

    try {
        const response = await _utility.HttpSend(method, url, payload);
        if (response.ok) {

            //get response
            const data = await response.data;

            //hold the chat Id
            chatId = data.chatId;

            appendMessage('system', data.message);
        }
    }
    finally {
        thinking.remove();
        toggleInputEnable(true);
    }
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
    chatId = null;
    document.getElementById('chat-output').innerHTML = "";
    document.getElementById('chat-input').value = "";
    await sendMessage("hi");
}

//async function audioChat() {
//    chatId = null;
//    document.getElementById('chat-output').innerHTML = "";
//    document.getElementById('chat-input').value = "";

//    //get agentId & sessionId 
//    const method = 'GET';
//    const url = `{urlBland}/blandwebclientconfig`;

//    try {
//        const response = await _utility.HttpSend(method, url, payload);
//        if (response.ok) {

//            //get response
//            const data = await response.data;
//            const blandClient = new BlandWebClient(
//                data.agentId,
//                data.token
//            );
//            await blandClient.initConversation({
//                sampleRate: 44100,
//            });

//        }
//    }
//    finally {
//        thinking.remove();
//        toggleInputEnable(true);
//    }
//}

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

document.addEventListener('DOMContentLoaded', async () => {
    //wire up event handlers
    document.getElementById('chat-send').addEventListener('click', appendAndSend);
    document.getElementById('chat-new').addEventListener('click', newChat);
    //document.getElementById('chat-audio').addEventListener('click', await audioChat);
    document.addEventListener('keydown', function (event) {
        if (event.key === 'Enter') {
            appendAndSend();
        }
    });

    //init
    await newChat();
});
