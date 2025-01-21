//import { useEffect, useState } from 'react';
import './App.css';
import { BrowserRouter, Route, Routes } from 'react-router-dom';
import LoginPortal from './components/LoginPortal';

function App() {
    return (
        <div className="App">
            <BrowserRouter>
                <Routes>
                    <Route path="/" element={<LoginPortal />} />
                </Routes>
            </BrowserRouter>
        </div>
    );
}

export default App;