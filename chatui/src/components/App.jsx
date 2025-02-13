import React, { useState, useEffect, useRef } from 'react';
import ReactMarkdown from 'react-markdown';
import ChatService from '../services/ChatService';
import './App.css'; // Import the CSS file

const App = () => {
    const [messages, setMessages] = useState([]);
    const [prompt, setPrompt] = useState('');
    const [chats, setChats] = useState([]);
    const [selectedChatId, setSelectedChatId] = useState(null);
    const [loadingChats, setLoadingChats] = useState(true);
    const messagesEndRef = useRef(null);
    const [newChatName, setNewChatName] = useState('');

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

    const handleChatSelect = async (chatId) => {
        setSelectedChatId(chatId);
        try {
            const data = await chatService.getChatMessages(chatId);
            setMessages(data);
        } catch (error) {
            console.error('Error fetching chat messages:', error);
        }
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!prompt.trim() || !selectedChatId) return;

        // Add the user's message
        const userMessage = { id: Date.now(), sender: 'user', text: prompt };
        setMessages(prevMessages => [...prevMessages, userMessage]);

        // Show loading indicator
        const loadingIndicatorId = Date.now() + 1;
        setMessages(prevMessages => [
            ...prevMessages,
            { id: loadingIndicatorId, sender: 'assistant', text: 'Loading...', isLoading: true }
        ]);

        try {
            const stream = chatService.sendPrompt(selectedChatId, prompt);

            setPrompt('');

            let firstChunk = true;
            for await (const chunk of stream) {
                if (firstChunk) {
                    // Remove loading indicator and add the bot message with the first chunk
                    setMessages(prev =>
                        prev.filter(msg => msg.id !== loadingIndicatorId).concat({
                            id: loadingIndicatorId,
                            sender: 'assistant',
                            text: chunk,
                            isLoading: false
                        })
                    );
                    firstChunk = false;
                } else {
                    // Update the bot message with subsequent chunks
                    setMessages(prev =>
                        prev.map(msg =>
                            msg.id === loadingIndicatorId ? { ...msg, text: msg.text + chunk } : msg
                        )
                    );
                }
            }
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

    useEffect(() => {
        if (messagesEndRef.current) {
            messagesEndRef.current.scrollTop = messagesEndRef.current.scrollHeight;
        }
    }, [messages]);

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
                            {chat.name}
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
                        disabled={!selectedChatId}
                        className="message-input"
                    />
                    <button type="submit" disabled={!selectedChatId} className="message-button">
                        Send
                    </button>
                </form>
            </div>
        </div>
    );
};

export default App;