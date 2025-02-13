import React, { useState, useEffect, useRef } from 'react';
import ReactMarkdown from 'react-markdown';
import ChatService from '../services/ChatService';

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
        <div style={{ display: 'flex', maxWidth: '900px', margin: '2rem auto', fontFamily: 'Arial, sans-serif' }}>
            <div style={{ width: '250px', flexShrink: 0, marginRight: '1rem' }}>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                    <h2>Chats</h2>
                    {loadingChats && <p>Loading...</p>}
                </div>
                <ul style={{ listStyle: 'none', padding: 0, flexGrow: 1 }}>
                    {chats.map(chat => (
                        <li
                            key={chat.id}
                            onClick={() => handleChatSelect(chat.id)}
                            style={{
                                padding: '10px',
                                cursor: 'pointer',
                                backgroundColor: selectedChatId === chat.id ? '#007aff' : '#f9f9f9',
                                color: selectedChatId === chat.id ? '#fff' : '#000',
                                borderRadius: '5px',
                                marginBottom: '5px'
                            }}
                        >
                            {chat.name}
                        </li>
                    ))}
                </ul>
                <form onSubmit={handleNewChatSubmit} style={{ marginTop: '1rem' }}>
                    <input
                        type="text"
                        value={newChatName}
                        onChange={e => setNewChatName(e.target.value)}
                        placeholder="New chat name"
                        style={{
                            width: '100%',
                            padding: '10px',
                            borderRadius: '20px',
                            border: '1px solid #ccc',
                            marginBottom: '10px',
                            boxSizing: 'border-box'
                        }}
                    />
                    <button
                        type="submit"
                        style={{
                            width: '100%',
                            padding: '10px',
                            borderRadius: '20px',
                            border: 'none',
                            background: '#007aff',
                            color: '#fff',
                            cursor: 'pointer'
                        }}
                    >
                        Create Chat
                    </button>
                </form>
            </div>
            <div style={{ flexGrow: 1 }}>
                <h1 style={{ textAlign: 'center', color: '#333' }}>Chat with Our Bot</h1>
                <div
                    ref={messagesEndRef}
                    style={{
                        border: '1px solid #ccc',
                        borderRadius: '8px',
                        padding: '1rem',
                        height: '400px',
                        overflowY: 'auto',
                        background: '#f9f9f9'
                    }}
                >
                    {messages.map(msg => (
                        <div
                            key={msg.id}
                            style={{
                                display: 'flex',
                                justifyContent: msg.sender === 'assistant' ? 'flex-start' : 'flex-end',
                                marginBottom: '1rem'
                            }}
                        >
                            <div
                                style={{
                                    background: msg.sender === 'assistant' ? '#e5e5ea' : '#007aff',
                                    color: msg.sender === 'assistant' ? '#000' : '#fff',
                                    padding: '10px 15px',
                                    borderRadius: '20px',
                                    maxWidth: '70%'
                                }}
                            >
                                <ReactMarkdown>{msg.text}</ReactMarkdown>
                            </div>
                        </div>
                    ))}
                </div>
                <form onSubmit={handleSubmit} style={{ display: 'flex', marginTop: '1rem' }}>
                    <input
                        type="text"
                        value={prompt}
                        onChange={e => setPrompt(e.target.value)}
                        placeholder="Enter your message..."
                        disabled={!selectedChatId}
                        style={{
                            flexGrow: 1,
                            padding: '10px',
                            borderRadius: '20px',
                            border: '1px solid #ccc',
                            marginRight: '10px',
                            backgroundColor: !selectedChatId ? '#e0e0e0' : '#fff'
                        }}
                    />
                    <button
                        type="submit"
                        disabled={!selectedChatId}
                        style={{
                            padding: '10px 20px',
                            borderRadius: '20px',
                            border: 'none',
                            background: !selectedChatId ? '#ccc' : '#007aff',
                            color: '#fff',
                            cursor: !selectedChatId ? 'not-allowed' : 'pointer'
                        }}
                    >
                        Send
                    </button>
                </form>
            </div>
        </div>
    );
};

export default App;