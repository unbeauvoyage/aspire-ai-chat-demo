import React, { useState, useEffect, useRef, startTransition } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import ChatService from '../services/ChatService';
import Sidebar from './Sidebar';
import ChatContainer from './ChatContainer';
import './App.css';

const loadingIndicatorId = 'loading-indicator';

const App = () => {
    const [messages, setMessages] = useState([]);
    const [prompt, setPrompt] = useState('');
    const [chats, setChats] = useState([]);
    const [selectedChatId, setSelectedChatId] = useState(null);
    const selectedChatIdRef = useRef(null);
    const [loadingChats, setLoadingChats] = useState(true);
    const messagesEndRef = useRef(null);
    const [newChatName, setNewChatName] = useState('');
    const abortControllerRef = useRef(null);
    const [shouldAutoScroll, setShouldAutoScroll] = useState(true);
    const [streamingMessageId, setStreamingMessageId] = useState(null);
    const { chatId } = useParams();
    const navigate = useNavigate();

    const chatService = new ChatService('/api/chat');

    useEffect(() => {
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
    }, [chatId]);

    useEffect(() => {
        if (chatId) {
            handleChatSelect(chatId);
        }
    }, [chatId]);

    const onSelectChat = (id) => {
        navigate(`/chat/${id}`);
    };

    const handleChatSelect = async (id) => {
        setSelectedChatId(id);
        selectedChatIdRef.current = id;
        // Clear the message list immediately on chat switch
        setMessages([]);
        let lastMessageId = null;
        try {
            const data = await chatService.getChatMessages(id);
            const filteredMessages = data.filter(msg => msg.text && msg.sender === 'assistant');
            const lastMessage = filteredMessages.length > 0 ? filteredMessages[filteredMessages.length - 1] : null;
            lastMessageId = lastMessage ? lastMessage.id : null;

            setMessages(data);
            setTimeout(() => scrollToBottom('instant'), 100);
        } catch (error) {
            console.error('Error fetching chat messages:', error);
        }

        const streamChat = async (chatId) => {
            if (abortControllerRef.current) {
                abortControllerRef.current.abort();
            }
            abortControllerRef.current = new AbortController();
            const currentChatId = chatId;

            try {
                const stream = chatService.stream(chatId, lastMessageId, abortControllerRef.current);
                for await (const { id, sender, text, isFinal } of stream) {
                    if (selectedChatIdRef.current !== currentChatId) break;
                    console.debug('Received chunk:', id, sender, text, isFinal);
                    updateMessageById(id, text, sender, isFinal);
                    if (isFinal) {
                        setStreamingMessageId(null);
                    } else {
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
            }
            finally {
                setStreamingMessageId(null);
            }
        };

        streamChat(id);
    };

    const updateMessageById = (id, newText, sender, isFinal = false) => {
        setMessages(prevMessages => {
            const lastUserMessage = prevMessages.filter(m => m.sender === 'user').slice(-1)[0];
            if (isFinal && lastUserMessage && lastUserMessage.text === newText) {
                return prevMessages;
            }
            const existingMessage = prevMessages.find(msg => msg.id === id);
            if (existingMessage) {
                return prevMessages.map(msg =>
                    msg.id === id
                        ? {
                            ...msg,
                            text: (msg.text === 'Generating reply...' ? newText : msg.text + newText),
                            isLoading: false,
                            sender: sender || msg.sender
                        }
                        : msg
                );
            } else {
                return prevMessages
                    .filter(msg => msg.id !== loadingIndicatorId)
                    .concat({
                        id,
                        sender: sender || 'assistant',
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

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!prompt.trim() || !selectedChatId) return;
        if (streamingMessageId) return;

        const userMessage = { id: Date.now(), sender: 'user', text: prompt };
        setMessages(prevMessages => [...prevMessages, userMessage]);

        setMessages(prevMessages => [
            ...prevMessages,
            { id: loadingIndicatorId, sender: 'assistant', text: 'Generating reply...', isLoading: true }
        ]);

        try {
            chatService.sendPrompt(selectedChatId, prompt);
            setPrompt('');
        } catch (error) {
            console.error('Streaming error:', error);
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
            onSelectChat(newChat.id);
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

    const cancelChat = () => {
        chatService.cancelChat(streamingMessageId);
    };

    return (
        <div className="app-container">
            <Sidebar
                chats={chats}
                selectedChatId={selectedChatId}
                loadingChats={loadingChats}
                newChatName={newChatName}
                setNewChatName={setNewChatName}
                handleNewChatSubmit={handleNewChatSubmit}
                handleDeleteChat={handleDeleteChat}
                onSelectChat={onSelectChat}
            />
            <ChatContainer
                messages={messages}
                prompt={prompt}
                setPrompt={setPrompt}
                handleSubmit={handleSubmit}
                cancelChat={cancelChat}
                streamingMessageId={streamingMessageId}
                messagesEndRef={messagesEndRef}
                shouldAutoScroll={shouldAutoScroll}
            />
        </div>
    );
};

export default App;