import React, { createContext, useState } from 'react';

export const AuthContext = createContext();

export function AuthProvider({ children }) {
    const [authState, setAuthState] = useState({
        accessToken: null
    });

    const setAuth = (accessToken) => {
        setAuthState({ accessToken});
    };

    const clearAuth = () => {
        setAuthState({ accessToken: null});
    };

    return (
        <AuthContext.Provider value={{ authState, setAuth, clearAuth }}>
            {children}
        </AuthContext.Provider>
    );
}
