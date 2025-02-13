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

    async *sendPrompt(id, prompt) {
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

        if (!response.body) {
            throw new Error('ReadableStream not supported in this browser.');
        }

        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        let done = false;
        let buffer = '';

        while (!done) {
            const { value, done: streamDone } = await reader.read();
            done = streamDone;
            if (value) {
                buffer += decoder.decode(value, { stream: true });
                let parts = buffer.split('\n\n');
                buffer = parts.pop(); // Keep the last incomplete part in the buffer
                for (let part of parts) {
                    if (part.startsWith('data: ')) {
                        yield part.substring(6).replace(/\\n/g, '\n'); // Remove 'data: ' prefix and replace escaped newlines
                    }
                }
            }
        }
    }
}

export default ChatService;
