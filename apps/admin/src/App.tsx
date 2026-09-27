import { useEffect, useState } from 'react';
import { Link, Route, Routes } from 'react-router-dom';
import { apiRequest } from './api';
import type { HouseholdAccount, HouseholdGuest } from './types';

function countGuestResponses(guests: HouseholdGuest[]) {
  return guests.filter((guest) => guest.attendance === 'ATTENDING').length;
}

function DashboardPage() {
  const [summary, setSummary] = useState<{ households: number; guests: number; confirmedGuests: number; respondedHouseholds: number } | null>(null);

  useEffect(() => {
    const adminKey = localStorage.getItem('justine-et-fabien-admin-key') ?? 'demo-admin-key';
    apiRequest<{ households: number; guests: number; confirmedGuests: number; respondedHouseholds: number }>('organizer/dashboard', { method: 'GET' }, adminKey)
      .then((data) => setSummary(data))
      .catch(() => setSummary({ households: 0, guests: 0, confirmedGuests: 0, respondedHouseholds: 0 }));
  }, []);

  const households = summary ? Array.from({ length: summary.households }, () => ({ id: '', householdName: '', email: '', adults: [], children: [], rsvp: {}, passwordHash: '', passwordSalt: '', createdAt: '', updatedAt: '' })) : [];
  const totalGuests = summary?.guests ?? 0;
  const confirmedGuests = summary?.confirmedGuests ?? 0;
  const householdsWithResponse = summary?.respondedHouseholds ?? 0;

  return (
    <main className="admin-shell">
      <aside className="sidebar">
        <h2>Wedding Admin</h2>
        <nav>
          <Link to="/">Dashboard</Link>
          <Link to="/households">Households</Link>
          <Link to="/gallery">Gallery</Link>
        </nav>
      </aside>

      <section className="content-panel">
        <h1>Organizer dashboard</h1>
        <div className="cards">
          <article className="panel-card">
            <h3>RSVP status</h3>
            <p>{confirmedGuests} confirmed</p>
          </article>
          <article className="panel-card">
            <h3>Households</h3>
            <p>{households.length} active</p>
          </article>
          <article className="panel-card">
            <h3>Guests</h3>
            <p>{totalGuests} tracked</p>
          </article>
          <article className="panel-card">
            <h3>Responded</h3>
            <p>{householdsWithResponse} households</p>
          </article>
        </div>
      </section>
    </main>
  );
}

function HouseholdsPage() {
  const [households, setHouseholds] = useState<HouseholdAccount[]>([]);

  useEffect(() => {
    const adminKey = localStorage.getItem('justine-et-fabien-admin-key') ?? 'demo-admin-key';
    apiRequest<{ households: HouseholdAccount[] }>('organizer/households', { method: 'GET' }, adminKey)
      .then((data) => setHouseholds(data.households ?? []))
      .catch(() => setHouseholds([]));
  }, []);

  return (
    <main className="admin-shell">
      <aside className="sidebar">
        <h2>Wedding Admin</h2>
        <nav>
          <Link to="/">Dashboard</Link>
          <Link to="/households">Households</Link>
          <Link to="/gallery">Gallery</Link>
        </nav>
      </aside>

      <section className="content-panel">
        <h1>Households</h1>
        {households.length === 0 ? (
          <p className="empty-state">No household accounts have been registered yet.</p>
        ) : (
          <div className="household-list">
            {households.map((household) => {
              const guestCount = household.adults.length + household.children.length;
              const responseMap = Object.values(household.rsvp ?? {});
              const householdStatus = responseMap.every((status) => status === 'NOT_ANSWERED') ? 'Pending' : 'Responded';

              return (
                <article className="household-card" key={household.id}>
                  <div className="household-header">
                    <div>
                      <h3>{household.householdName}</h3>
                      <p>{household.email}</p>
                    </div>
                    <span className={`pill ${householdStatus === 'Pending' ? 'neutral' : 'success'}`}>{householdStatus}</span>
                  </div>

                  <div className="meta-grid">
                    <div>
                      <strong>Guests</strong>
                      <span>{guestCount}</span>
                    </div>
                    <div>
                      <strong>Ceremony</strong>
                      <span>{household.rsvp?.ceremony ?? 'NOT_ANSWERED'}</span>
                    </div>
                    <div>
                      <strong>Dinner</strong>
                      <span>{household.rsvp?.dinner ?? 'NOT_ANSWERED'}</span>
                    </div>
                    <div>
                      <strong>Brunch</strong>
                      <span>{household.rsvp?.brunch ?? 'NOT_ANSWERED'}</span>
                    </div>
                  </div>
                </article>
              );
            })}
          </div>
        )}
      </section>
    </main>
  );
}

function GalleryPage() {
  return (
    <main className="admin-shell">
      <aside className="sidebar">
        <h2>Wedding Admin</h2>
        <nav>
          <Link to="/">Dashboard</Link>
          <Link to="/households">Households</Link>
          <Link to="/gallery">Gallery</Link>
        </nav>
      </aside>
      <section className="content-panel">
        <h1>Gallery moderation</h1>
        <p className="empty-state">Placeholder upload and approval queue for post-wedding gallery.</p>
      </section>
    </main>
  );
}

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<DashboardPage />} />
      <Route path="/households" element={<HouseholdsPage />} />
      <Route path="/gallery" element={<GalleryPage />} />
    </Routes>
  );
}
