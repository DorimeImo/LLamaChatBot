import axios from 'axios';
import { AuthContext } from './GlobalAuthContext';
import React, { useContext } from 'react';

const useAxios = () => {
    const { authState, setAuth, clearAuth } = useContext(AuthContext);

    const axiosInstance = axios.create({
        baseURL: '/api',
        withCredentials: true,
    });

    axiosInstance.interceptors.request.use(
        (config) => {
            if (authState.accessToken) {
                config.headers['Authorization'] = `Bearer ${authState.accessToken}`;
            }
            return config;
        },
        (error) => {
            return Promise.reject(error);
        }
    );

    // Response interceptor to handle 401 errors and refresh token
    axiosInstance.interceptors.response.use(
        (response) => response,
        async (error) => {
            const originalRequest = error.config;

            if (error.response.status === 401 && !originalRequest._retry) {
                originalRequest._retry = true;
                try {
                    //Dmitry : here we need to pass user id and refresh token in request
                    const refreshResponse = await axios.post(
                        '/api/Auth/refresh',
                        { userId: authState.userId }, // Include userId in the refresh request
                        { withCredentials: true }
                    );
                    const { accessToken } = refreshResponse.data;

                    setAuth(accessToken, authState.userId);

                    originalRequest.headers['Authorization'] = `Bearer ${accessToken}`;
                    return axiosInstance(originalRequest);
                } catch (refreshError) {
                    clearAuth();
                    navigate('/login');
                    return Promise.reject(refreshError);
                }
            }

            return Promise.reject(error);
        }
    );

    return axiosInstance;
};

export default useAxios;