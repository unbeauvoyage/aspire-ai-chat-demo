import React, { useState, useEffect, useRef } from 'react';
import ChatService from '../services/ChatService';

const App = () => {
    const [messages, setMessages] = useState([
        { id: 1, sender: 'bot', text: 'Hello! How can I help you today?' }
    ]);
    const [prompt, setPrompt] = useState('');
    const messagesEndRef = useRef(null);

    const backendUrl = '/api'

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!prompt.trim()) return;

        // Add the user's message
        const userMessage = { id: Date.now(), sender: 'user', text: prompt };
        setMessages(prevMessages => [...prevMessages, userMessage]);

        // Create a new bot message with empty text that will be updated as responses stream in
        const botMessageId = Date.now() + 1;
        setMessages(prevMessages => [
            ...prevMessages,
            { id: botMessageId, sender: 'bot', text: '' }
        ]);

        try {
            const chatService = new ChatService(backendUrl);
            const stream = chatService.sendPrompt(prompt);

            setPrompt('');

            for await (const chunk of stream) {
                setMessages(prev =>
                    prev.map(msg =>
                        msg.id === botMessageId ? { ...msg, text: msg.text + chunk } : msg
                    )
                );
            }
        } catch (error) {
            console.error('Streaming error:', error);
            // Optionally update the bot message with an error message
            setMessages(prev =>
                prev.map(msg =>
                    msg.id === botMessageId ? { ...msg, text: msg.text + '\n[Error in receiving response]' } : msg
                )
            );
        }
    };

    useEffect(() => {
        if (messagesEndRef.current) {
            messagesEndRef.current.scrollTop = messagesEndRef.current.scrollHeight;
        }
    }, [messages]);

    return (
        <div style={{ maxWidth: '600px', margin: '2rem auto', fontFamily: 'Arial, sans-serif' }}>
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
                            justifyContent: msg.sender === 'bot' ? 'flex-start' : 'flex-end',
                            marginBottom: '1rem'
                        }}
                    >
                        <div
                            style={{
                                background: msg.sender === 'bot' ? '#e5e5ea' : '#007aff',
                                color: msg.sender === 'bot' ? '#000' : '#fff',
                                padding: '10px 15px',
                                borderRadius: '20px',
                                maxWidth: '70%'
                            }}
                        >
                            {msg.text}
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
                    style={{
                        flexGrow: 1,
                        padding: '10px',
                        borderRadius: '20px',
                        border: '1px solid #ccc',
                        marginRight: '10px'
                    }}
                />
                <button
                    type="submit"
                    style={{
                        padding: '10px 20px',
                        borderRadius: '20px',
                        border: 'none',
                        background: '#007aff',
                        color: '#fff',
                        cursor: 'pointer'
                    }}
                >
                    Send
                </button>
            </form>
        </div>
    );
};

export default App;