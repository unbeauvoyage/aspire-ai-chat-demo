import React, { useState, useEffect, useRef } from 'react';
import ReactMarkdown from 'react-markdown';
import ChatService from '../services/ChatService';
import './App.css';

const loadingIndicatorId = 'loading-indicator';

const App = () => {
    const [messages, setMessages] = useState([]);
    const [prompt, setPrompt] = useState('');
    const [chats, setChats] = useState([]);
    const [selectedChatId, setSelectedChatId] = useState(null);
    const [loadingChats, setLoadingChats] = useState(true);
    const messagesEndRef = useRef(null);
    const [newChatName, setNewChatName] = useState('');
    const abortControllerRef = useRef(null);
    const [shouldAutoScroll, setShouldAutoScroll] = useState(true);
    const [streamingMessageId, setStreamingMessageId] = useState(null);

    const backendUrl = '/api';
    const chatService = new ChatService(backendUrl);

    useEffect(() => {
        // Fetch the list of chats when the component mounts
        const fetchChats = async () => {
            try {
                const data = await chatService.getChats();
                setChats(data);
            } catch (error) {
                console.error('Error fetching chats:', error);
            } finally {
                setLoadingChats(false);
            }
        };

        fetchChats();
    }, []);

    const updateMessageById = (id, newText) => {
        setMessages(prevMessages => {
            const existingMessage = prevMessages.find(msg => msg.id === id);
            if (existingMessage) {
                return prevMessages.map(msg =>
                    msg.id === id
                        ? {
                            ...msg,
                            text: (msg.text === 'Generating reply...' ? newText : msg.text + newText),
                            isLoading: false
                        }
                        : msg
                );
            } else {
                return prevMessages
                    .filter(msg => msg.id !== loadingIndicatorId)
                    .concat({
                        id,
                        sender: 'assistant',
                        text: newText,
                        isLoading: false
                    });
            }
        });
    };

    const scrollToBottom = (behavior = 'smooth') => {
        if (messagesEndRef.current && shouldAutoScroll) {
            messagesEndRef.current.scrollTo({
                top: messagesEndRef.current.scrollHeight,
                behavior
            });
        }
    };

    const handleScroll = (e) => {
        const container = e.target;
        const isNearBottom = container.scrollHeight - container.scrollTop - container.clientHeight < 100;
        setShouldAutoScroll(isNearBottom);
    };

    useEffect(() => {
        const container = messagesEndRef.current;
        if (container) {
            container.addEventListener('scroll', handleScroll);
            return () => container.removeEventListener('scroll', handleScroll);
        }
    }, []);

    useEffect(() => {
        if (shouldAutoScroll) {
            scrollToBottom();
        }
    }, [messages]);

    const handleChatSelect = async (chatId) => {
        setSelectedChatId(chatId);
        let lastMessageId = null;
        try {
            const data = await chatService.getChatMessages(chatId);

            // Filter for messages with text (and optionally matching sender) then take last one.
            const filteredMessages = data.filter(msg => msg.text && msg.sender === 'assistant');
            const lastMessage = filteredMessages.length > 0 ? filteredMessages[filteredMessages.length - 1] : null;
            lastMessageId = lastMessage ? lastMessage.id : null;

            setMessages(data);
            // Force scroll to bottom on chat selection, using instant scroll
            setTimeout(() => scrollToBottom('instant'), 100);
        } catch (error) {
            console.error('Error fetching chat messages:', error);
        }

        const streamChat = async (id) => {
            if (abortControllerRef.current) {
                abortControllerRef.current.abort();
            }
            abortControllerRef.current = new AbortController();

            try {
                const stream = chatService.stream(id, lastMessageId, abortControllerRef.current);
                for await (const { id, text, isFinal } of stream) {
                    console.debug('Received chunk:', id, text, isFinal);
                    updateMessageById(id, text);
                    if (isFinal) {
                        setStreamingMessageId(null);
                    } else {
                        // Only set streamingMessageId if not already set
                        setStreamingMessageId(current => current ? current : id);
                    }
                }
            } catch (error) {
                if (error.name !== 'AbortError') {
                    console.error('Streaming error:', error);
                    setMessages(prev =>
                        prev.map(msg =>
                            msg.id === loadingIndicatorId ? { ...msg, text: '[Error in receiving response]', isLoading: false } : msg
                        )
                    );
                }
                setStreamingMessageId(null);
            }
        };

        streamChat(chatId);
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!prompt.trim() || !selectedChatId) return;
        // Prevent prompt submission while streaming.
        if (streamingMessageId) return;

        // Add the user's message
        const userMessage = { id: Date.now(), sender: 'user', text: prompt };
        setMessages(prevMessages => [...prevMessages, userMessage]);

        // Show loading indicator
        setMessages(prevMessages => [
            ...prevMessages,
            { id: loadingIndicatorId, sender: 'assistant', text: 'Generating reply...', isLoading: true }
        ]);

        try {
            chatService.sendPrompt(selectedChatId, prompt);

            setPrompt('');

        } catch (error) {
            console.error('Streaming error:', error);
            // Optionally update the bot message with an error message
            setMessages(prev =>
                prev.map(msg =>
                    msg.id === loadingIndicatorId ? { ...msg, text: '[Error in receiving response]', isLoading: false } : msg
                )
            );
        }
    };

    const handleNewChatSubmit = async (e) => {
        e.preventDefault();
        if (!newChatName.trim()) return;

        try {
            const newChat = await chatService.createChat(newChatName);
            setChats(prevChats => [...prevChats, newChat]);
            setNewChatName('');
            handleChatSelect(newChat.id);
        } catch (error) {
            console.error('Error creating new chat:', error);
        }
    };

    const handleDeleteChat = async (e, chatId) => {
        e.stopPropagation();
        try {
            await chatService.deleteChat(chatId);
            setChats(prevChats => prevChats.filter(chat => chat.id !== chatId));
            if (selectedChatId === chatId) {
                setSelectedChatId(null);
                setMessages([]);
            }
        } catch (error) {
            console.error('Error deleting chat:', error);
        }
    };

    const isNearBottom = () => {
        if (!messagesEndRef.current) return false;
        const container = messagesEndRef.current;
        return container.scrollHeight - container.scrollTop - container.clientHeight < 100;
    };

    // Replace cancelChat function
    const cancelChat = () => {
        chatService.cancelChat(streamingMessageId);
    };

    return (
        <div className="app-container">
            <div className="sidebar">
                <div className="sidebar-header">
                    <h2>Chats</h2>
                    {loadingChats && <p>Loading...</p>}
                </div>
                <ul className="chat-list">
                    {chats.map(chat => (
                        <li
                            key={chat.id}
                            onClick={() => handleChatSelect(chat.id)}
                            className={`chat-item ${selectedChatId === chat.id ? 'selected' : ''}`}
                        >
                            <span className="chat-name">{chat.name}</span>
                            <button
                                className="delete-chat-button"
                                onClick={(e) => handleDeleteChat(e, chat.id)}
                                title="Delete chat"
                            >
                                ×
                            </button>
                        </li>
                    ))}
                </ul>
                <form onSubmit={handleNewChatSubmit} className="new-chat-form">
                    <input
                        type="text"
                        value={newChatName}
                        onChange={e => setNewChatName(e.target.value)}
                        placeholder="New chat name"
                        className="new-chat-input"
                    />
                    <button type="submit" className="new-chat-button">
                        Create Chat
                    </button>
                </form>
            </div>
            <div className="chat-container">
                <div ref={messagesEndRef} className="messages-container">
                    {messages.map(msg => (
                        <div key={msg.id} className={`message ${msg.sender}`}>
                            <div className="message-content">
                                <ReactMarkdown>{msg.text}</ReactMarkdown>
                            </div>
                        </div>
                    ))}
                </div>
                <form onSubmit={handleSubmit} className="message-form">
                    <input
                        type="text"
                        value={prompt}
                        onChange={e => setPrompt(e.target.value)}
                        placeholder="Enter your message..."
                        disabled={!selectedChatId || streamingMessageId}
                        className="message-input"
                    />
                    {streamingMessageId ? (
                        <button type="button" onClick={cancelChat} className="message-button">
                            Stop
                        </button>
                    ) : (
                        <button type="submit" disabled={!selectedChatId || streamingMessageId} className="message-button">
                            Send
                        </button>
                    )}
                </form>
            </div>
        </div>
    );
};

export default App;