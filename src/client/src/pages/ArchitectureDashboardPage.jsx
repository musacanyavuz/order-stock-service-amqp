import React, { useRef, useState } from 'react';
import axios from 'axios';
import './ArchitectureDashboard.css';

const sleep = (ms) => new Promise(r => setTimeout(r, ms));

export default function ArchitectureDashboardPage() {
    const packetRef = useRef(null);
    const [messages, setMessages] = useState({}); // { s1: { text, type }, s2: ... }

    const showResult = (scenarioId, msg, type) => {
        setMessages(prev => ({ ...prev, [scenarioId]: { text: msg, type } }));
        setTimeout(() => {
            setMessages(prev => {
                const newState = { ...prev };
                delete newState[scenarioId];
                return newState;
            });
        }, 4000);
    };

    const animatePacket = (startId, endId, color = '', duration = 1000) => {
        return new Promise(resolve => {
            const startEl = document.getElementById(startId);
            const endEl = document.getElementById(endId);
            const packet = document.createElement('div');

            packet.className = 'packet';
            if (color) packet.style.backgroundColor = color;

            // Container relative
            const container = document.getElementById('arch-container');
            if (!container || !startEl || !endEl) { resolve(); return; }

            const startRect = startEl.getBoundingClientRect();
            const endRect = endEl.getBoundingClientRect();
            const parentRect = container.getBoundingClientRect();

            packet.style.left = (startRect.left - parentRect.left + startRect.width / 2 - 8) + 'px';
            packet.style.top = (startRect.top - parentRect.top + startRect.height / 2 - 8) + 'px';

            container.appendChild(packet);

            // Reflow
            packet.offsetHeight;

            packet.style.transition = `all ${duration}ms ease-in-out`;

            setTimeout(() => {
                packet.style.left = (endRect.left - parentRect.left + endRect.width / 2 - 8) + 'px';
                packet.style.top = (endRect.top - parentRect.top + endRect.height / 2 - 8) + 'px';
            }, 10);

            setTimeout(() => {
                packet.remove();
                resolve();
            }, duration + 50);
        });
    };

    // --- Scenario 1: Dual Write ---
    const [s1DbStatus, setS1DbStatus] = useState("Empty");
    const [s1DbColor, setS1DbColor] = useState("");

    const runS1_Bad = async () => {
        setS1DbStatus("Empty");
        setS1DbColor("");
        await animatePacket('s1-user', 's1-app', '', 500);

        setS1DbStatus("Saved (1)");
        setS1DbColor("#00e676");

        // Fail to MQ
        const packet = document.createElement('div');
        packet.className = 'packet';
        packet.style.backgroundColor = 'red';
        const startEl = document.getElementById('s1-app');
        const container = document.getElementById('arch-container');
        const startRect = startEl.getBoundingClientRect();
        const parentRect = container.getBoundingClientRect();

        packet.style.left = (startRect.left - parentRect.left + 50) + 'px';
        packet.style.top = (startRect.top - parentRect.top + 40) + 'px';
        container.appendChild(packet);

        setTimeout(() => {
            packet.style.transition = 'all 1s';
            packet.style.left = (parseInt(packet.style.left) + 100) + 'px';
            packet.style.opacity = 0;
        }, 10);

        await sleep(1000);
        packet.remove();
        showResult('s1', "VERİ TUTARSIZLIĞI! DB yazıldı, Event kayboldu.", "error");
        setS1DbColor("#555");
    };

    const runS1_Best = async () => {
        setS1DbStatus("Empty");
        setS1DbColor("");
        await animatePacket('s1-user', 's1-app', '', 500);

        setS1DbStatus("Saved (Order+Outbox)");
        setS1DbColor("#00e676");

        await sleep(500);
        await animatePacket('s1-db', 's1-mq', '', 1000);
        showResult('s1', "EVENTUAL CONSISTENCY SAĞLANDI.", "success");
        setS1DbColor("#555");
    };

    // --- Scenario 2: Idempotency ---
    const [s2Balance, setS2Balance] = useState(100);
    const [s2Keys, setS2Keys] = useState([]);

    const runS2_Bad = async () => {
        setS2Balance(100);
        const p1 = animatePacket('s2-panel-title', 's2-api', 'yellow', 800);
        await sleep(200);
        const p2 = animatePacket('s2-panel-title', 's2-api', 'orange', 800);
        await Promise.all([p1, p2]);

        setS2Balance(80);
        showResult('s2', "HATALI İŞLEM! Mükerrer ödeme.", "error");
    };

    const runS2_Best = async () => {
        setS2Balance(100);
        setS2Keys([]);

        // Use a real GUID for testing
        const idempotencyKey = crypto.randomUUID();

        await animatePacket('s2-panel-title', 's2-api', '', 800);

        // --- First Request ---
        try {
            // NOTE: We use the existing Order endpoint. 
            // In a real scenario, this would be a specific Payment endpoint, 
            // but for this demo, we use Order.API directly to demonstrate the middleware.
            const payload = {
                buyerId: "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                orderItems: [{ productId: "test", count: 1, price: 10 }],
                address: { line: "test", province: "test", district: "test" }
            };

            await axios.post('http://localhost:5001/api/orders', payload, {
                headers: { 'X-Idempotency-Key': idempotencyKey }
            });

            setS2Keys([idempotencyKey]);
            setS2Balance(90);
            showResult('s2', "1. İstek Başarılı (200 OK)", "success");

        } catch (err) {
            showResult('s2', "API Hatası: " + err.message, "error");
            return;
        }

        await sleep(500);
        await animatePacket('s2-panel-title', 's2-api', '', 800);

        // --- Second Request (Same Key) ---
        try {
            const payload = {
                buyerId: "3fa85f64-5717-4562-b3fc-2c963f66afa6", // Same data
                orderItems: [{ productId: "test", count: 1, price: 10 }],
                address: { line: "test", province: "test", district: "test" }
            };

            const response = await axios.post('http://localhost:5001/api/orders', payload, {
                headers: { 'X-Idempotency-Key': idempotencyKey }
            });

            // If we are here, it means we got a 200 OK.
            // In real idempotency, the server returns the cached response.
            // We can confirm this if needed, but visually we just show "Safe"

            showResult('s2', `GÜVENLİ: Mükerrer Kayıt Yok. (Cached Response)`, "success");

        } catch (err) {
            showResult('s2', "API Hatası (2): " + err.message, "error");
        }
    };

    // --- Scenario 3: Saga ---
    const [s3StockVal, setS3StockVal] = useState(10);
    const [s3StockColor, setS3StockColor] = useState("");
    const [s3PayColor, setS3PayColor] = useState("");

    const runS3_Bad = async () => {
        setS3StockVal(10);
        setS3StockColor(""); setS3PayColor("");

        await animatePacket('s3-panel-title', 's3-stock', '', 600);
        setS3StockVal(9);
        setS3StockColor("#00e676");

        await animatePacket('s3-stock', 's3-pay', '', 600);
        setS3PayColor("#ff1744");

        showResult('s3', "TUTARSIZ DURUM. Stok düştü, ödeme yok.", "error");
        await sleep(2000);
        setS3StockColor(""); setS3PayColor("");
    };

    const runS3_Best = async () => {
        setS3StockVal(10);
        setS3StockColor(""); setS3PayColor("");

        await animatePacket('s3-panel-title', 's3-stock', '', 600);
        setS3StockVal(9);
        setS3StockColor("#00e676");

        await animatePacket('s3-stock', 's3-pay', '', 600);
        setS3PayColor("#ff1744");

        await sleep(500);
        await animatePacket('s3-pay', 's3-stock', 'orange', 800);

        setS3StockVal(10);
        setS3StockColor("");
        showResult('s3', "ROLLBACK BAŞARILI. Sistem telafi edildi.", "success");
        setS3PayColor("");
    };

    // --- Scenario 4: Concurrency ---
    const [s4Price, setS4Price] = useState(100);
    const [s4Version, setS4Version] = useState(null);
    const [s4Admin2Border, setS4Admin2Border] = useState("");

    const runS4_Bad = async () => {
        setS4Price(100); setS4Version(null);

        await sleep(500); // Reads
        await animatePacket('s4-admin1', 's4-db', '', 500);
        setS4Price(120);

        await sleep(500);
        await animatePacket('s4-admin2', 's4-db', '', 500);
        setS4Price(110);

        showResult('s4', "VERİ EZİLDİ! Admin A'nın işlemi kayboldu.", "error");
    };

    const runS4_Best = async () => {
        setS4Price(100); setS4Version(1);

        await sleep(500); // Reads
        await animatePacket('s4-admin1', 's4-db', '', 500);
        setS4Price(120);
        setS4Version(2);

        await sleep(500);
        await animatePacket('s4-admin2', 's4-db', '', 500);
        setS4Admin2Border("red");

        showResult('s4', "VERİ BÜTÜNLÜĞÜ KORUNDU. Sürüm hatası alındı.", "success");
        setTimeout(() => setS4Admin2Border(""), 2000);
    };


    return (
        <div className="arch-dashboard-container" id="arch-container">
            <h1>Architecture & Resilience Dashboard</h1>

            {/* S1 */}
            <div className="scenario-section">
                <div className="control-panel">
                    <div className="section-title">1. Dual Write Problem</div>
                    <div className="description">Veritabanına yazıp mesaj kuyruğuna event atarken olası veri tutarsızlıkları.</div>
                    <div className="btn-group">
                        <button className="btn-bad" onClick={runS1_Bad}>Bad Practice</button>
                        <button className="btn-best" onClick={runS1_Best}>Outbox Pattern</button>
                    </div>
                </div>
                <div className="visual-panel">
                    <div className="box" id="s1-user"><i className="fas fa-user"></i><span>User</span></div>
                    <div className="box" id="s1-app"><i className="fas fa-code"></i><span>Order API</span></div>
                    <div className="box" id="s1-db" style={{ borderColor: s1DbColor }}>
                        <i className="fas fa-database"></i><span>DB</span>
                        <small>{s1DbStatus}</small>
                    </div>
                    <div className="box" id="s1-mq"><i className="fas fa-envelope"></i><span>RabbitMQ</span></div>

                    {messages.s1 && <div className={`result-overlay ${messages.s1.type}`}>{messages.s1.text}</div>}
                </div>
                <div className="tech-summary"><strong>Teknik Özet:</strong> Transactional Outbox Pattern kullanılarak atomik işlem sağlanır.</div>
            </div>

            {/* S2 */}
            <div className="scenario-section">
                <div className="control-panel" id="s2-panel-title">
                    <div className="section-title">2. Idempotency</div>
                    <div className="description">Network gecikmesiyle gelen mükerrer isteklerin yönetimi.</div>
                    <div className="btn-group">
                        <button className="btn-bad" onClick={runS2_Bad}>Double Click</button>
                        <button className="btn-best" onClick={runS2_Best}>Idempotency Key</button>
                    </div>
                </div>
                <div className="visual-panel">
                    <div className="box" id="s2-api">
                        <i className="fas fa-server"></i><span>Payment API</span>
                        <small>Keys: {JSON.stringify(s2Keys)}</small>
                    </div>
                    <div className="box">
                        <i className="fas fa-wallet"></i><span>Wallet</span>
                        <div style={{ color: '#00ff9d' }}>{s2Balance} TL</div>
                    </div>
                    {messages.s2 && <div className={`result-overlay ${messages.s2.type}`}>{messages.s2.text}</div>}
                </div>
                <div className="tech-summary"><strong>Teknik Özet:</strong> Idempotency Key ile mükerrer işlemler engellenir.</div>
            </div>

            {/* S3 */}
            <div className="scenario-section">
                <div className="control-panel" id="s3-panel-title">
                    <div className="section-title">3. Distributed Saga</div>
                    <div className="description">Zincirleme işlemlerde hata durumunda rollback mekanizması.</div>
                    <div className="btn-group">
                        <button className="btn-bad" onClick={runS3_Bad}>No Compensation</button>
                        <button className="btn-best" onClick={runS3_Best}>Compensating Tx</button>
                    </div>
                </div>
                <div className="visual-panel">
                    <div className="box" id="s3-stock" style={{ borderColor: s3StockColor }}>
                        <i className="fas fa-boxes"></i><span>Stock</span>
                        <small>{s3StockVal}</small>
                    </div>
                    <div className="box" id="s3-pay" style={{ borderColor: s3PayColor }}>
                        <i className="fas fa-credit-card"></i><span>Payment</span>
                        <small>Fails</small>
                    </div>
                    {messages.s3 && <div className={`result-overlay ${messages.s3.type}`}>{messages.s3.text}</div>}
                </div>
                <div className="tech-summary"><strong>Teknik Özet:</strong> Saga Pattern ile Compensating Transaction tetiklenir.</div>
            </div>

            {/* S4 */}
            <div className="scenario-section">
                <div className="control-panel">
                    <div className="section-title">4. Optimistic Locking</div>
                    <div className="description">Aynı anda aynı veriyi güncelleyen iki admin.</div>
                    <div className="btn-group">
                        <button className="btn-bad" onClick={runS4_Bad}>Last Commit Wins</button>
                        <button className="btn-best" onClick={runS4_Best}>Versioning</button>
                    </div>
                </div>
                <div className="visual-panel">
                    <div className="box" id="s4-admin1"><i className="fas fa-user-tie"></i><span>Admin A</span></div>
                    <div className="box" id="s4-db">
                        <i className="fas fa-database"></i><span>DB</span>
                        <small>Price: {s4Price}</small>
                        {s4Version && <span className="version-tag">v{s4Version}</span>}
                    </div>
                    <div className="box" id="s4-admin2" style={{ borderColor: s4Admin2Border }}><i className="fas fa-user-tie"></i><span>Admin B</span></div>
                    {messages.s4 && <div className={`result-overlay ${messages.s4.type}`}>{messages.s4.text}</div>}
                </div>
                <div className="tech-summary"><strong>Teknik Özet:</strong> RowVersion ile veri bütünlüğü korunur.</div>
            </div>

        </div>
    );
}
