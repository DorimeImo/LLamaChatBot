import React, { createContext, useState } from 'react';

export const AuthContext = createContext();

export function AuthProvider({ children }) {
    const [authState, setAuthState] = useState({
        accessToken: null,
        userId: null,
    });

    const setAuth = (accessToken, userId) => {
        setAuthState({ accessToken, userId });
    };

    const clearAuth = () => {
        setAuthState({ accessToken: null, userId: null });
    };

    return (
        <AuthContext.Provider value={{ authState, setAuth, clearAuth }}>
            {children}
        </AuthContext.Provider>
    );
}
