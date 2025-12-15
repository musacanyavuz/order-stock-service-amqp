import React, { useState, useEffect, useRef } from 'react';
import './BackendDashboard.css';

const sleep = (ms) => new Promise(r => setTimeout(r, ms));

export default function BackendDashboardPage() {
    // --- 1. Race Condition ---
    const [balance, setBalance] = useState(100);
    const [raceLog, setRaceLog] = useState("Simülasyon bekleniyor...");
    const mutexLockRef = useRef(false);
    const balanceRef = useRef(100); // For race condition ref sharing simulation

    async function processWithdraw(useLock) {
        if (useLock) {
            while (mutexLockRef.current) { await sleep(10); }
            mutexLockRef.current = true;
        }

        // READ
        let current = balanceRef.current; // access shared state
        await sleep(Math.random() * 50 + 20);

        // WRITE
        balanceRef.current = current - 10;
        setBalance(balanceRef.current);

        if (useLock) {
            mutexLockRef.current = false;
        }
    }

    async function startRace(useLock) {
        balanceRef.current = 100;
        setBalance(100);
        setRaceLog("İşlemler başladı...");

        const promises = [];
        for (let i = 0; i < 10; i++) {
            promises.push(processWithdraw(useLock));
        }

        await Promise.all(promises);

        const final = balanceRef.current;
        if (final === 0) {
            setRaceLog("Başarılı! Bakiye sıfırlandı.");
        } else {
            setRaceLog(`Race Condition! Beklenen: 0, Sonuç: ${final}`);
        }
    }

    // --- 2. Circuit Breaker ---
    const [isApiUp, setIsApiUp] = useState(true);
    const [circuitState, setCircuitState] = useState('CLOSED'); // CLOSED, OPEN, HALF_OPEN
    const [failureCount, setFailureCount] = useState(0);
    const cbTimeoutRef = useRef(null);

    const toggleApi = () => setIsApiUp(!isApiUp);

    const makeCircuitRequest = () => {
        if (circuitState === 'OPEN') {
            alert("Circuit is OPEN! İstekler reddediliyor.");
            return;
        }

        try {
            // Using logic instead of real fetch for simulation
            if (!isApiUp) throw new Error("API Down");

            alert("İstek Başarılı!");
            if (circuitState === 'HALF_OPEN') {
                setCircuitState('CLOSED');
                setFailureCount(0);
            }
        } catch (e) {
            const newCount = failureCount + 1;
            setFailureCount(newCount);

            if (circuitState === 'HALF_OPEN') {
                setCircuitState('OPEN');
                setResetTimer();
            } else if (newCount >= 3) {
                setCircuitState('OPEN');
                setResetTimer();
            } else {
                alert("İstek Başarısız! (Hata sayacı arttı)");
            }
        }
    };

    const setResetTimer = () => {
        if (cbTimeoutRef.current) clearTimeout(cbTimeoutRef.current);
        cbTimeoutRef.current = setTimeout(() => {
            setCircuitState('HALF_OPEN');
        }, 5000);
    };

    // --- 3. Cache vs DB ---
    const [dataContent, setDataContent] = useState('Veri Yok');
    const [fetchTime, setFetchTime] = useState(0);
    const [dataSourceClass, setDataSourceClass] = useState('');
    const [cachedData, setCachedData] = useState(null);

    const fetchData = async () => {
        setDataContent("Yükleniyor...");
        const start = performance.now();

        if (cachedData) {
            setDataContent(`${cachedData} (Cache)`);
            setDataSourceClass('cache');
        } else {
            await sleep(2000);
            const data = "Kullanıcı Verisi #123";
            setCachedData(data);
            setDataContent(`${data} (Database)`);
            setDataSourceClass('db');
        }
        setFetchTime(Math.round(performance.now() - start));
    };

    const clearCache = () => {
        setCachedData(null);
        setDataContent("Veri Yok");
        setDataSourceClass('');
        setFetchTime(0);
    };

    // --- 4. Message Queue ---
    const [queue, setQueue] = useState([]);
    const [processed, setProcessed] = useState(0);
    const [isWorking, setIsWorking] = useState(false);

    const produceTasks = () => {
        const newTasks = Array.from({ length: 5 }, (_, i) => ({ id: Date.now() + i }));
        setQueue(prev => [...prev, ...newTasks]);
    };

    useEffect(() => {
        const interval = setInterval(async () => {
            if (queue.length > 0 && !isWorking) {
                setIsWorking(true);
                await sleep(800);
                setQueue(prev => prev.slice(1));
                setProcessed(prev => prev + 1);
                setIsWorking(false);
            }
        }, 1000);
        return () => clearInterval(interval);
    }, [queue, isWorking]);

    // --- 5. Load Balancer ---
    const [serverCounts, setServerCounts] = useState([0, 0, 0]);
    const [activeServer, setActiveServer] = useState(null);
    const [isTrafficOn, setIsTrafficOn] = useState(false);
    const serverIndexRef = useRef(0);
    const trafficIntervalRef = useRef(null);

    const startTraffic = () => {
        if (isTrafficOn) return;
        setIsTrafficOn(true);
        trafficIntervalRef.current = setInterval(() => {
            const current = serverIndexRef.current;
            serverIndexRef.current = (serverIndexRef.current + 1) % 3;

            setServerCounts(prev => {
                const newCounts = [...prev];
                newCounts[current]++;
                return newCounts;
            });
            setActiveServer(current);
            setTimeout(() => setActiveServer(null), 150);

        }, 200);
    };

    const stopTraffic = () => {
        if (trafficIntervalRef.current) clearInterval(trafficIntervalRef.current);
        setIsTrafficOn(false);
    };

    return (
        <div className="dashboard-container">
            <h1>Backend Concepts Dashboard</h1>
            <div className="dashboard-grid">

                {/* 1. Race Condition */}
                <div className="card">
                    <div className="card-header">
                        <i className="fas fa-money-bill-wave"></i>
                        <span className="card-title">Race Condition Arena</span>
                    </div>
                    <div className="card-content">
                        <div className="display-box">
                            Bakiye: <span className="balance-display">{balance}</span> TL
                        </div>
                        <div className="controls">
                            <button className="btn-danger" onClick={() => startRace(false)}>
                                <i className="fas fa-lock-open"></i> Lock Yok (Hatalı)
                            </button>
                            <button className="btn-success" onClick={() => startRace(true)}>
                                <i className="fas fa-lock"></i> Lock Var (Güvenli)
                            </button>
                        </div>
                        <div className="race-log" dangerouslySetInnerHTML={{ __html: raceLog }}></div>
                    </div>
                </div>

                {/* 2. Circuit Breaker */}
                <div className="card">
                    <div className="card-header">
                        <i className="fas fa-car-battery"></i>
                        <span className="card-title">Circuit Breaker Switch</span>
                    </div>
                    <div className="card-content">
                        <div className="display-box" style={{ display: 'flex', justifyContent: 'space-between' }}>
                            <div>
                                <small>API Durumu</small><br />
                                <span className={`status-badge ${isApiUp ? 'green' : 'red'}`}>{isApiUp ? 'UP' : 'DOWN'}</span>
                            </div>
                            <div>
                                <small>Circuit</small><br />
                                <span className={`status-badge ${circuitState === 'OPEN' ? 'red' : circuitState === 'HALF_OPEN' ? 'yellow' : 'green'}`}>
                                    {circuitState}
                                </span>
                            </div>
                        </div>
                        <div className="circuit-diagram">
                            <div style={{ fontSize: '2rem' }}><i className="fas fa-plug"></i></div>
                            <div className={`connector ${circuitState === 'OPEN' ? 'open' : ''}`}></div>
                            <div style={{ fontSize: '2rem' }}><i className="fas fa-server"></i></div>
                        </div>
                        <div className="controls">
                            <button className="btn-warning" onClick={toggleApi}>
                                <i className="fas fa-random"></i> API Boz/Düzelt
                            </button>
                            <button className="btn-primary" onClick={makeCircuitRequest}>
                                <i className="fas fa-paper-plane"></i> İstek Gönder
                            </button>
                        </div>
                        <div className="race-log">Hata Sayısı: {failureCount}</div>
                    </div>
                </div>

                {/* 3. Cache vs DB */}
                <div className="card">
                    <div className="card-header">
                        <i className="fas fa-database"></i>
                        <span className="card-title">Cache vs Database Duel</span>
                    </div>
                    <div className="card-content">
                        <div className={`display-box data-source-indicator ${dataSourceClass}`}>
                            <i className="fas fa-box"></i> {dataContent}
                        </div>
                        <div style={{ textAlign: 'center', color: '#888', fontSize: '0.8rem' }}>Süre: {fetchTime}ms</div>
                        <div className="controls">
                            <button className="btn-primary" onClick={fetchData}>
                                <i className="fas fa-download"></i> Veriyi Getir
                            </button>
                            <button className="btn-danger" onClick={clearCache}>
                                <i className="fas fa-trash"></i> Cache Temizle
                            </button>
                        </div>
                    </div>
                </div>

                {/* 4. MQ Factory */}
                <div className="card">
                    <div className="card-header">
                        <i className="fas fa-conveyor-belt"></i>
                        <span className="card-title">Message Queue Factory</span>
                    </div>
                    <div className="card-content">
                        <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.8rem' }}>
                            <span>Producer</span>
                            <span>Queue: {queue.length}</span>
                            <span>Processed: {processed}</span>
                        </div>
                        <div className="mq-container">
                            {queue.map(item => (
                                <div key={item.id} className="mq-box">T</div>
                            ))}
                        </div>
                        <div className={`worker ${isWorking ? 'working' : ''}`}>
                            <i className="fas fa-cog"></i>
                        </div>
                        <div className="controls">
                            <button className="btn-primary" onClick={produceTasks}>
                                <i className="fas fa-plus"></i> 5 Task Ekle
                            </button>
                        </div>
                    </div>
                </div>

                {/* 5. Load Balancer */}
                <div className="card">
                    <div className="card-header">
                        <i className="fas fa-sitemap"></i>
                        <span className="card-title">Load Balancer Roulette</span>
                    </div>
                    <div className="card-content">
                        <div style={{ textAlign: 'center' }}>
                            <i className="fas fa-users" style={{ fontSize: '2rem', color: '#aaa' }}></i>
                            <div style={{ fontSize: '0.8rem' }}>Traffic</div>
                        </div>
                        <div className="servers-container">
                            {[0, 1, 2].map(idx => (
                                <div key={idx} className={`server-box ${activeServer === idx ? 'active' : ''}`}>
                                    <i className="fas fa-server"></i>
                                    <small>{['A', 'B', 'C'][idx]}</small>
                                    <span style={{ color: 'var(--primary-color)' }}>{serverCounts[idx]}</span>
                                </div>
                            ))}
                        </div>
                        <div className="controls" style={{ marginTop: '15px' }}>
                            <button className="btn-success"
                                onMouseDown={startTraffic}
                                onMouseUp={stopTraffic}
                                onMouseLeave={stopTraffic}>
                                <i className="fas fa-play"></i> Basılı Tut
                            </button>
                        </div>
                    </div>
                </div>

            </div>
        </div>
    );
}
