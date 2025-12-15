import { useState, useEffect, useRef } from 'react'
import axios from 'axios'
import * as signalR from '@microsoft/signalr'

export default function TestClientPage() {
    const [activeTab, setActiveTab] = useState('manual') // manual | periodic

    // Manual State
    const [basketCount, setBasketCount] = useState(1)

    // Periodic State
    const [intervalMs, setIntervalMs] = useState(2000)
    const [isPeriodicRunning, setIsPeriodicRunning] = useState(false)
    const intervalRef = useRef(null)

    // Logs & Notifications
    const [logs, setLogs] = useState([])
    const [notifications, setNotifications] = useState({}) // { orderId: [events] }
    const [isLoading, setIsLoading] = useState(false)

    // Refs for auto-scroll
    const logsEndRef = useRef(null)
    const notificationsEndRef = useRef(null)

    // Auto-scroll effects
    useEffect(() => {
        logsEndRef.current?.scrollIntoView({ behavior: "smooth" })
    }, [logs])

    // Note: Notifications object changes keys, so we scroll on new orders
    useEffect(() => {
        // notification-grid is reversed in render currently, but for auto-scroll "down" 
        // usually implies seeing the latest. 
        // If the user wants to see the NEWEST item which is at the TOP (reverse map),
        // then "scroll down" might be confusing. 
        // However, usually logs scroll to bottom. 
        // Let's assume standard log behavior: Newest at bottom if normal map, Newest at top if reverse map.
        // The current render uses .reverse(). So newest is at TOP. 
        // If Newest is at TOP, scroll should stay at TOP (0,0).
        // BUT the user asked for "auto scroll down". 
        // This implies the list grows DOWNWARDS.
        // Let's REMOVE .reverse() from the render so latest is at the BOTTOM, satisfying "scroll down".

        notificationsEndRef.current?.scrollIntoView({ behavior: "smooth" })
    }, [notifications])

    useEffect(() => {
        let isMounted = true;
        let localConnection = null;

        const startConnection = async () => {
            const connection = new signalR.HubConnectionBuilder()
                .withUrl("http://localhost:5003/notificationHub")
                .withAutomaticReconnect()
                .build();

            localConnection = connection;

            try {
                await connection.start();
                if (isMounted) {
                    console.log('Connected to Notification Hub');
                    addLog('System', 'Connected to Notification Hub');
                }
            } catch (err) {
                console.error('Connection failed: ', err);
                if (isMounted) {
                    addLog('System', 'Connection failed. Retrying in 3s...');
                    setTimeout(startConnection, 3000); // This recursion might be risky but keeping logic similar as before
                }
                return;
            }

            connection.on("ReceiveNotification", (message) => {
                if (!isMounted) return;
                setNotifications(prev => {
                    const orderId = message.orderId;
                    const currentEvents = prev[orderId] || [];
                    // Prevent duplicate events
                    if (currentEvents.some(e => e.timestamp === message.timestamp && e.type === message.type)) {
                        return prev;
                    }
                    return {
                        ...prev,
                        [orderId]: [...currentEvents, message]
                    };
                });
            });
        };

        startConnection();

        return () => {
            isMounted = false;
            if (localConnection) {
                localConnection.stop().catch(e => console.error("Error stopping connection:", e));
            }
        };
    }, []);

    // Periodic Logic
    useEffect(() => {
        if (isPeriodicRunning) {
            intervalRef.current = setInterval(() => {
                // Create 1 order periodically
                createOrders(1, true);
            }, intervalMs);
        } else {
            if (intervalRef.current) clearInterval(intervalRef.current);
        }

        return () => {
            if (intervalRef.current) clearInterval(intervalRef.current);
        };
    }, [isPeriodicRunning, intervalMs]);


    const addLog = (source, msg) => {
        setLogs(prev => [`[${new Date().toLocaleTimeString()}] ${source}: ${msg}`, ...prev]);
    }

    // Idempotency State
    const [idempotencyKey, setIdempotencyKey] = useState(crypto.randomUUID())



    const resetStocks = async () => {
        try {
            addLog('System', 'Resetting stocks...');
            await axios.post('http://localhost:5002/api/stocks/reset');
            addLog('System', 'Stocks reset to 100!');
        } catch (error) {
            console.error(error);
            addLog('System', `Stock reset failed: ${error.message}`);
            alert('Stok yenileme başarısız. Stock.API ayakta mı?');
        }
    }

    const createOrders = async (count, isAuto = false) => {
        if (!isAuto) setIsLoading(true);
        addLog('Client', `Creating ${count} orders...`);

        const payload = {
            buyerId: "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            orderItems: [
                {
                    productId: "d8d47424-0c5a-4e2b-b5d1-93335555d444",
                    count: 1,
                    price: 100
                }
            ],
            address: {
                line: "Istiklal Cad",
                province: "Istanbul",
                district: "Beyoglu"
            }
        }



        const promises = []
        for (let i = 0; i < count; i++) {
            promises.push(
                axios.post('http://localhost:5001/api/orders', payload)
                    .then(res => {
                        // Initialize notification group for this order to show it immediately
                        setNotifications(prev => ({
                            ...prev,
                            [res.data.orderId]: [{ type: 'OrderSent', message: 'Order request sent from client', timestamp: new Date().toISOString() }]
                        }));
                        return `Order Sent: ${res.data.orderId}`;
                    })
                    .catch(err => `Order Failed: ${err.message}`)
            )
        }

        try {
            const results = await Promise.all(promises);
            results.forEach(r => addLog('Client', r));
        } catch (error) {
            addLog('Client', `Error: ${error.message}`);
        } finally {
            if (!isAuto) setIsLoading(false);
        }
    }

    return (
        <div className="page-container">
            <h1>Beymen E-Commerce Test Client</h1>


            {/* NEW: Stock Management Panel */}
            <div className="controls-card" style={{ marginBottom: '20px', borderLeft: '5px solid #ffc107' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <h3 style={{ margin: 0 }}>⚠️ Stock Management</h3>
                    <div style={{ display: 'flex', gap: '10px' }}>
                        <button onClick={resetStocks} style={{ backgroundColor: '#ffc107', color: '#000' }}>
                            🔄 Reset Stocks (100)
                        </button>
                    </div>
                </div>
            </div>


            <div className="controls-card">
                <div className="tabs">
                    <button className={activeTab === 'manual' ? 'active' : ''} onClick={() => setActiveTab('manual')}>Manuel</button>
                    <button className={activeTab === 'periodic' ? 'active' : ''} onClick={() => setActiveTab('periodic')}>Otomatik (Periyodik)</button>
                </div>

                <div className="tab-content">
                    {activeTab === 'manual' && (
                        <div className="control-group">
                            <label>
                                Sepet Adedi:
                                <select value={basketCount} onChange={(e) => setBasketCount(Number(e.target.value))}>
                                    <option value="1">1</option>
                                    <option value="10">10</option>
                                    <option value="50">50</option>
                                    <option value="100">100</option>
                                </select>
                            </label>
                            <button onClick={() => createOrders(basketCount)} disabled={isLoading || isPeriodicRunning}>
                                {isLoading ? 'Creating...' : 'Siparişleri Oluştur'}
                            </button>
                        </div>
                    )}

                    {activeTab === 'periodic' && (
                        <div className="control-group">
                            <label>
                                Sıklık (ms):
                                <input type="number" value={intervalMs} onChange={(e) => setIntervalMs(Number(e.target.value))} min="100" />
                            </label>
                            <button
                                onClick={() => setIsPeriodicRunning(!isPeriodicRunning)}
                                style={{ backgroundColor: isPeriodicRunning ? '#dc3545' : '#28a745' }}
                            >
                                {isPeriodicRunning ? 'Durdur' : 'Başlat'}
                            </button>
                        </div>
                    )}
                </div>
            </div>

            <div style={{ display: 'flex', gap: '20px', marginTop: '20px' }}>
                <div className="logs-panel" style={{ flex: 1 }}>
                    <h3>Process Logs</h3>
                    <div className="scroll-box">
                        {logs.map((log, index) => (
                            <div key={index} className="log-line">{log}</div>
                        ))}
                        <div ref={logsEndRef} />
                    </div>
                </div>

                <div className="notifications-panel" style={{ flex: 2 }}>
                    <h3>Real-time Order Flow ({Object.keys(notifications).length})</h3>
                    <div className="scroll-box notification-grid">
                        {Object.entries(notifications).map(([orderId, events]) => (
                            <div key={orderId} className="order-card">
                                <div className="order-header">
                                    <strong>{orderId.includes('-') && orderId.length > 30 ? 'Order:' : 'Request:'}</strong> {orderId}
                                </div>
                                <div className="order-events">
                                    {events.map((ev, idx) => (
                                        <div key={idx} style={{ marginBottom: '5px' }}>
                                            <div className={`event-badge ${ev.Type || ev.type}`}>
                                                {ev.Source === 'NotificationService' && '🔔 '}
                                                {ev.Type || ev.type}
                                            </div>
                                            {/* Hide message for OrderCreated and StockReserved as requested */}
                                            {(ev.message || ev.Message) && !['OrderCreated', 'StockReserved'].includes(ev.Type || ev.type) && (
                                                <div style={{ fontSize: '0.8em', color: '#555', marginTop: '2px', marginLeft: '5px' }}>
                                                    {ev.message || ev.Message}
                                                </div>
                                            )}
                                        </div>
                                    ))}
                                </div>
                            </div>
                        ))}
                        <div ref={notificationsEndRef} />
                    </div>
                </div>
            </div>
        </div>
    )
}
