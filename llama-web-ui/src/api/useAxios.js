import { useContext, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import axios from 'axios';
import { AuthContext } from '../context/AuthContext';
import { setupInterceptors } from './setupInterceptors';

const useAxios = () => {
    const authContext = useContext(AuthContext);
    const navigate = useNavigate();

    const axiosInstance = useMemo(() => {
        const instance = axios.create({
            baseURL: 'http://localhost:7224/api',
            withCredentials: true,
        });

        setupInterceptors(instance, authContext, navigate);
        return instance;
    }, [authContext, navigate]); 

    return axiosInstance;
};

export default useAxios;