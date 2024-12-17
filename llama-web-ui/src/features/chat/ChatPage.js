import React, { useState, useRef, useCallback } from 'react';
import useAuth from '../auth/AuthUtility';

function ChatPage() {
    const { isAuthenticated } = useAuth();
    const [message, setMessage] = useState(''); //useState reflects changes of these 2 properties message and messages to the user.
    const [messages, setMessages] = useState([]);

    const appendMessage = (eventData) => {
        setMessages((prev) => {
            if (prev.length > 0 && prev[prev.length - 1].sender === 'LLama') {
                const updatedMessages = [...prev];
                updatedMessages[updatedMessages.length - 1].text += eventData;               
                eventData = '';
                return updatedMessages;
            }
            return [...prev, { sender: 'LLama', text: eventData }];
        });
    };

    const sendMessage = async () => {
        if (!isAuthenticated || !message.trim()) return;

        setMessages((prev) => [...prev, { sender: 'You', text: message }]);
        setMessage('');

        try {
            const eventSource = new EventSource(`https://localhost:7224/api/Streaming/stream?accessToken=your_token&message=${encodeURIComponent(message)}`);
            eventSource.onmessage = (event) => {
                appendMessage(event.data);
            };

            eventSource.onerror = () => {
                console.error('Error occurred with the event source.');
                eventSource.close();
            };
        } catch (error) {
            console.error('Failed to send the message:', error);
        }
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
                <button onClick={sendMessage}>Send Message</button>
            </div>
        </div>
    );
}

export default ChatPage;