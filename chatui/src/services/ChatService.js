import { streamJsonValues } from './stream';

class ChatService {
    constructor(backendUrl) {
        this.backendUrl = backendUrl;
    }

    async getChats() {
        const response = await fetch(`${this.backendUrl}/chat`);
        if (!response.ok) {
            throw new Error('Error fetching chats');
        }
        return await response.json();
    }

    async getChatMessages(chatId) {
        const response = await fetch(`${this.backendUrl}/chat/${chatId}`);
        if (!response.ok) {
            throw new Error('Error fetching chat messages');
        }
        return await response.json();
    }

    async createChat(name) {
        const response = await fetch(`${this.backendUrl}/chat`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ name })
        });
        if (!response.ok) {
            throw new Error('Failed to create chat');
        }
        return await response.json();
    }

    async *stream(id, lastMessageId, abortController) {
        const response = await fetch(`${this.backendUrl}/chat/stream/${id}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ lastMessageId }),
            signal: abortController.signal
        });
        if (!response.ok) {
            throw new Error('Error fetching chat stream');
        }

        if (!response.body) {
            throw new Error('ReadableStream not supported in this browser.');
        }

        for await (const value of streamJsonValues(response, abortController.signal)) {
            yield { id: value.id, text: value.text, isFinal: value.isFinal ?? false };
        }
    }

    async sendPrompt(id, prompt) {
        const response = await fetch(`${this.backendUrl}/chat/${id}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ text: prompt })
        });

        if (!response.ok) {
            let errorMessage;
            try {
                errorMessage = await response.text();
            } catch (e) {
                errorMessage = response.statusText;
            }
            throw new Error(`Error sending prompt: ${errorMessage}`);
        }
    }

    async deleteChat(id) {
        const response = await fetch(`${this.backendUrl}/chat/${id}`, {
            method: 'DELETE'
        });

        if (!response.ok) {
            throw new Error('Failed to delete chat');
        }
    }

    async cancelChat(id) {
        const response = await fetch(`${this.backendUrl}/chat/${id}/cancel`, {
            method: 'POST'
        });
        if (!response.ok) {
            throw new Error('Failed to cancel chat');
        }
    }
}

export default ChatService;
