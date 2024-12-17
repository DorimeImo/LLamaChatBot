import React from 'react';
import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import  ChatPage  from './features/chat/ChatPage';
import { AuthProvider } from './features/auth/GlobalAuthContext';
import ProtectedRoute from './ProtectedRoute';
import LoginPage from './features/auth/LoginPage';
import RegisterPage from './features/auth/RegisterPage';

function App() {
    return (
        <AuthProvider>
            <Router>
                <Header />
                <main style={{ flex: 1 }}>
                    <Routes>
                        <Route path="/login" element={<LoginPage />} />
                        <Route path="/register" element={<RegisterPage />} />
                        <Route
                            path="/chat"
                            element={
                                <ProtectedRoute>
                                    <ChatPage />
                                </ProtectedRoute>
                            }
                        />
                    </Routes>
                </main>
                <Footer />
            </Router>
        </AuthProvider>
    );
}

export default App;
