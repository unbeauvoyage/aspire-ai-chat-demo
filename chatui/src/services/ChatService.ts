import { Chat, Message, MessageFragment } from '../types/ChatTypes';
import * as signalR from '@microsoft/signalr';

class ChatService {
    private static instance: ChatService;
    private hubConnection?: signalR.HubConnection;
    private initialized = false;
    private backendUrl: string;

    private constructor(backendUrl: string) {
        this.backendUrl = backendUrl;
    }

    static getInstance(backendUrl: string): ChatService {
        if (!ChatService.instance) {
            ChatService.instance = new ChatService(backendUrl);
        }
        return ChatService.instance;
    }

    async ensureInitialized(): Promise<void> {
        if (!this.hubConnection && !this.initialized) {
            this.hubConnection = new signalR.HubConnectionBuilder()
                .withUrl(`${this.backendUrl}/stream`, {
                    skipNegotiation: true,
                    transport: signalR.HttpTransportType.WebSockets
                })
                .withStatefulReconnect()
                .withAutomaticReconnect({
                    nextRetryDelayInMilliseconds: retryContext => {
                        // We want to retry forever
                        if (retryContext.elapsedMilliseconds < 15 * 1000) {
                            // Max 15 seconds delay
                            return 15000;
                        }

                        // Otherwise, we want to retry every second
                        return retryContext.previousRetryCount * 1000;
                    }
                })
                .build();

            await this.hubConnection.start();
            this.initialized = true;
        }
    }

    async getChats(): Promise<Chat[]> {
        const response = await fetch(`${this.backendUrl}`);
        if (!response.ok) {
            throw new Error('Error fetching chats');
        }
        return await response.json();
    }

    async getChatMessages(chatId: string): Promise<Message[]> {
        const response = await fetch(`${this.backendUrl}/${chatId}`);
        if (!response.ok) {
            throw new Error('Error fetching chat messages');
        }
        return await response.json();
    }

    async createChat(name: string): Promise<Chat> {
        const response = await fetch(`${this.backendUrl}`, {
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

    async stream(
        id: string,
        lastMessageId: string,
        lastFragmentId: string,
        abortController: AbortController,
        onMessage: (fragment: MessageFragment) => void,
        onComplete?: () => void,
        onError?: (error: Error) => void
    ): Promise<void> {

        await this.ensureInitialized();

        if (!this.hubConnection) {
            throw new Error('ChatService not initialized');
        }

        const streamContext = { lastMessageId, lastFragmentId };
        const subscription = this.hubConnection.stream("Stream", id, streamContext)
            .subscribe({
                next: (value) => {
                    const fragment: MessageFragment = {
                        id: value.id,
                        sender: value.sender,
                        text: value.text,
                        isFinal: value.isFinal ?? false,
                        fragmentId: value.fragmentId
                    };
                    onMessage(fragment);
                },
                complete: () => {
                    console.debug(`Stream completed for chat: ${id}`);
                    onComplete?.();
                },
                error: (err) => {
                    console.error(`Stream error for chat: ${id}:`, err);
                    onError?.(err);
                }
            });

        abortController.signal.addEventListener('abort', () => {
            subscription.dispose();
        });
    }

    async sendPrompt(id: string, prompt: string): Promise<void> {
        const response = await fetch(`${this.backendUrl}/${id}`, {
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

    async deleteChat(id: string): Promise<void> {
        const response = await fetch(`${this.backendUrl}/${id}`, {
            method: 'DELETE'
        });

        if (!response.ok) {
            throw new Error('Failed to delete chat');
        }
    }

    async cancelChat(id: string): Promise<void> {
        const response = await fetch(`${this.backendUrl}/${id}/cancel`, {
            method: 'POST'
        });
        if (!response.ok) {
            throw new Error('Failed to cancel chat');
        }
    }
}

export default ChatService;
