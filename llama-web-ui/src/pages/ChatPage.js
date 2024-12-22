import React, { useState, useRef, useContext, useEffect, } from 'react';
import useAuth from '../hooks/useAuth';
import { AuthContext } from '../context/AuthContext';

function ChatPage() {
    const { isAuthenticated } = useAuth();
    const { authState } = useContext(AuthContext);
    const [message, setMessage] = useState(''); 
    const [messages, setMessages] = useState([]);
    const webSocketRef = useRef(null);
    const reconnectIntervalRef = useRef(1000);

    const appendMessage = (eventData) => {
        setMessages((prev) => {
            if (prev.length > 0 && prev[prev.length - 1].sender === 'LLama') {
                const updatedMessages = [...prev];
                updatedMessages[updatedMessages.length - 1].text += eventData;               
                return updatedMessages;
            }
            return [...prev, { sender: 'LLama', text: eventData }];
        });
    };

    useEffect(() => {
        console.log('WebSocket 1sr step.');
        if (isAuthenticated && authState.accessToken) {
            console.log('WebSocket 2nd step.');
            const connectWebSocket = () => {
                
                const ws = new WebSocket('ws://localhost:7224/api/wschat');

                ws.onopen = () => {
                    console.log('WebSocket connection established.');
                    //reconnectIntervalRef.current = 1000; 

                    const authPayload = {
                        type: 'authenticate',
                        token: authState.accessToken
                    };
                    ws.send(JSON.stringify(authPayload));
                };

                ws.onmessage = (event) => {
                    console.log('Received:', event.data);
                    appendMessage(event.data);
                };

                ws.onerror = (error) => {
                    console.error('WebSocket error:', error);
                };

                ws.onclose = (event) => {
                    console.log('WebSocket connection closed:', event.reason);
                    setTimeout(() => {
                        reconnectIntervalRef.current = Math.min(reconnectIntervalRef.current * 2, 30000); // Max 30 seconds
                        connectWebSocket();
                    }, reconnectIntervalRef.current);
                };

                webSocketRef.current = ws;
            };

            connectWebSocket();

            return () => {
                if (webSocketRef.current) {
                    webSocketRef.current.close();
                }
            };
        }
    }, [isAuthenticated, authState.accessToken]);

    const sendMessage = () => {
        if (!isAuthenticated || !message.trim() || !webSocketRef.current || webSocketRef.current.readyState !== WebSocket.OPEN) return;

        setMessages((prev) => [...prev, { sender: 'You', text: message }]);
        setMessage('');

        const payload = {
            type: 'message',
            text: message
        };
        webSocketRef.current.send(JSON.stringify(payload));
    };

    return (
        <div style={{ display: 'flex', flexDirection: 'column', height: '100vh' }}>
            <div style={{ flex: 1, overflowY: 'auto', padding: '10px' }}>
                <h1>Chat Page</h1>
                <div>

                {messages.map((msg, index) => (
                    <div key={index} style={{ margin: '5px 0' }}>
                        <strong>{msg.sender}:</strong>
                        <div>
                            {msg.text.split('\\n').map((line, i) => (
                                <React.Fragment key={i}>
                                    {line}
                                    {i < msg.text.split('\\n').length - 1 && <br />}
                                </React.Fragment>
                            ))}
                        </div>
                    </div>
                ))}

                </div>
            </div>
            <div style={{ display: 'flex', padding: '10px', borderTop: '1px solid #ccc' }}>
                <textarea
                    value={message}
                    onChange={(e) => setMessage(e.target.value)}
                    placeholder={isAuthenticated ? 'Type your message...' : 'Login to enable chat'}
                    disabled={!isAuthenticated}
                    style={{ flex: 1, marginRight: '10px' }}
                />
                <button 
                    onClick={sendMessage} 
                    disabled={!isAuthenticated || !webSocketRef.current || webSocketRef.current.readyState !== WebSocket.OPEN}
                >
                    Send Message
                </button>
            </div>
        </div>
    );
}

export default ChatPage;