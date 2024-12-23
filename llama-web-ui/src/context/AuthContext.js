import React, { createContext, useState } from 'react';

export const AuthContext = createContext();

export function AuthProvider({ children }) {
    const [authState, setAuthState] = useState({
        accessToken: null
    });

    const [isAuthenticated, setIsAuthenticated] = useState(false);
    const [loading, setLoading] = useState(true);

    const setAuth = (accessToken) => {
        setAuthState({ accessToken});
        setIsAuthenticated(true);
    };

    const clearAuth = () => {
        setAuthState({ accessToken: null});
        setIsAuthenticated(false);
    };

    return (
        <AuthContext.Provider value={{ authState, setAuth, clearAuth, isAuthenticated, loading, setLoading }}>
            {children}
        </AuthContext.Provider>
    );
}
