class ChatService {
    constructor(backendUrl) {
        this.backendUrl = backendUrl;
    }

    async *sendPrompt(prompt) {
        const response = await fetch(`${this.backendUrl}/chat`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ text: prompt })
        });

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
                        yield part.substring(6); // Remove 'data: ' prefix
                    }
                }
            }
        }
    }
}

export default ChatService;
