import { useEffect, useMemo, useState } from 'react';
import { Link, Route, Routes } from 'react-router-dom';
import {
  getCurrentUser,
  registerHousehold,
  signInHousehold,
  signOutHousehold,
  updateHousehold,
  type HouseholdAccount,
  type HouseholdGuest,
} from './auth';
import { content, type Locale } from './content';
import { getEditorialGalleryImages, getEditorialImageUrl } from './media';

const weddingDate = new Date('2027-06-14T16:00:00+02:00');

function getTimeLeft(target: Date) {
  const difference = target.getTime() - Date.now();

  if (difference <= 0) {
    return { days: 0, hours: 0, minutes: 0, seconds: 0, completed: true };
  }

  return {
    days: Math.floor(difference / (1000 * 60 * 60 * 24)),
    hours: Math.floor((difference / (1000 * 60 * 60)) % 24),
    minutes: Math.floor((difference / (1000 * 60)) % 60),
    seconds: Math.floor((difference / 1000) % 60),
    completed: false,
  };
}

function Countdown({ locale }: { locale: Locale }) {
  const [timeLeft, setTimeLeft] = useState(() => getTimeLeft(weddingDate));

  useEffect(() => {
    const timer = window.setInterval(() => {
      setTimeLeft(getTimeLeft(weddingDate));
    }, 1000);

    return () => window.clearInterval(timer);
  }, []);

  const copy = content[locale].countdown;

  return (
    <section className="countdown" aria-live="polite">
      <h2>{copy.label}</h2>
      {timeLeft.completed ? (
        <p className="countdown-finished">{copy.complete}</p>
      ) : (
        <div className="countdown-grid">
          <div><strong>{timeLeft.days}</strong><span>{copy.days}</span></div>
          <div><strong>{timeLeft.hours}</strong><span>{copy.hours}</span></div>
          <div><strong>{timeLeft.minutes}</strong><span>{copy.minutes}</span></div>
          <div><strong>{timeLeft.seconds}</strong><span>{copy.seconds}</span></div>
        </div>
      )}
    </section>
  );
}

function HomePage({ locale }: { locale: Locale }) {
  const copy = content[locale];
  const heroImage = getEditorialImageUrl(locale, 'heroImage');

  return (
    <main className="page-shell home-shell">
      <header className="hero">
        <div className="hero-layout">
          <div className="hero-copy">
            <p className="eyebrow">{copy.hero.intro}</p>
            <h1>{copy.hero.title}</h1>
            <p className="hero-subtitle">{copy.hero.date}</p>
            <p className="hero-location">{copy.hero.location}</p>
            <div className="cta-row">
              <Link to="/rsvp" className="button hero-button">{copy.hero.cta}</Link>
            </div>
          </div>

          <div className="hero-visual">
            {heroImage ? <img src={heroImage} alt={copy.hero.title} className="hero-image" /> : null}
          </div>
        </div>
      </header>

      <Countdown locale={locale} />

      <section className="info-grid">
        <article>
          <h2>{copy.sections.story}</h2>
          <p>We met in Paris, and every chapter since has felt like a quiet love story beginning to bloom.</p>
        </article>
        <article>
          <h2>{copy.sections.schedule}</h2>
          <p>Saturday 14 June 2027</p>
          <p>Ceremony starts at 4:00 PM</p>
        </article>
        <article>
          <h2>{copy.sections.info}</h2>
          <p>Dress code: elegant summer attire</p>
          <p>Children are welcome</p>
        </article>
      </section>
    </main>
  );
}

function RSVPPage({ locale }: { locale: Locale }) {
  const copy = content[locale].auth;
  const [tab, setTab] = useState<'register' | 'login'>('register');
  const [notice, setNotice] = useState<string | null>(null);
  const [currentUser, setCurrentUser] = useState<HouseholdAccount | null>(() => getCurrentUser());
  const [draftUser, setDraftUser] = useState<HouseholdAccount | null>(() => getCurrentUser());
  const [registerForm, setRegisterForm] = useState({ householdName: '', email: '', password: '', confirmPassword: '' });
  const [loginForm, setLoginForm] = useState({ email: '', password: '' });

  const handleRegister = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const result = await registerHousehold(registerForm);
    setNotice(result.message);
    if (result.success && result.user) {
      setCurrentUser(result.user);
      setDraftUser(result.user);
      setRegisterForm({ householdName: '', email: '', password: '', confirmPassword: '' });
      setTab('login');
    }
  };

  const handleLogin = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const result = await signInHousehold(loginForm);
    setNotice(result.message);
    if (result.success && result.user) {
      setCurrentUser(result.user);
      setDraftUser(result.user);
      setLoginForm({ email: '', password: '' });
    }
  };

  const handleSignOut = () => {
    signOutHousehold();
    setCurrentUser(null);
    setDraftUser(null);
    setNotice('Signed out successfully.');
  };

  const saveHouseholdChanges = () => {
    if (!draftUser) {
      return;
    }

    setCurrentUser(draftUser);
    updateHousehold(draftUser);
    setNotice('Household changes saved successfully.');
  };

  const updateGuest = (guestId: string, group: 'adults' | 'children', field: keyof HouseholdGuest, value: string) => {
    setDraftUser((previousUser) => {
      if (!previousUser) {
        return previousUser;
      }

      const edited = previousUser[group].map((guest) => {
        if (guest.id !== guestId) {
          return guest;
        }

        return { ...guest, [field]: value } as HouseholdGuest;
      });

      return { ...previousUser, [group]: edited } as HouseholdAccount;
    });
  };

  const updateAttendance = (guestId: string, group: 'adults' | 'children', field: 'attendance', value: HouseholdGuest['attendance']) => {
    setDraftUser((previousUser) => {
      if (!previousUser) {
        return previousUser;
      }

      const edited = previousUser[group].map((guest) => {
        if (guest.id !== guestId) {
          return guest;
        }

        return { ...guest, [field]: value };
      });

      return { ...previousUser, [group]: edited } as HouseholdAccount;
    });
  };

  const addGuest = (group: 'adults' | 'children') => {
    setDraftUser((previousUser) => {
      if (!previousUser) {
        return previousUser;
      }

      const nextGuest: HouseholdGuest = {
        id: crypto.randomUUID(),
        type: group === 'adults' ? 'ADULT' : 'CHILD',
        firstName: '',
        lastName: '',
        attendance: 'NOT_ANSWERED',
      };

      return { ...previousUser, [group]: [...previousUser[group], nextGuest] } as HouseholdAccount;
    });
  };

  const updateEventAttendance = (field: 'ceremony' | 'dinner' | 'brunch', value: HouseholdAccount['rsvp'][string]) => {
    setDraftUser((previousUser) => {
      if (!previousUser) {
        return previousUser;
      }

      return {
        ...previousUser,
        rsvp: {
          ...previousUser.rsvp,
          [field]: value,
        },
      } as HouseholdAccount;
    });
  };

  const household = draftUser ?? currentUser;

  return (
    <main className="page-shell narrow">
      <h1>{copy.title}</h1>

      {!household ? (
        <section className="card-stack">
          <div className="auth-tabs" role="tablist" aria-label="Authentication tabs">
            <button type="button" className={tab === 'register' ? 'active' : ''} onClick={() => setTab('register')}>{copy.registerTab}</button>
            <button type="button" className={tab === 'login' ? 'active' : ''} onClick={() => setTab('login')}>{copy.loginTab}</button>
          </div>

          {notice ? <p className="notice">{notice}</p> : null}

          {tab === 'register' ? (
            <form className="card-form" onSubmit={handleRegister}>
              <label>
                {copy.householdName}
                <input type="text" value={registerForm.householdName} onChange={(event) => setRegisterForm({ ...registerForm, householdName: event.target.value })} />
              </label>
              <label>
                {copy.email}
                <input type="email" value={registerForm.email} onChange={(event) => setRegisterForm({ ...registerForm, email: event.target.value })} />
              </label>
              <label>
                {copy.password}
                <input type="password" value={registerForm.password} onChange={(event) => setRegisterForm({ ...registerForm, password: event.target.value })} />
              </label>
              <label>
                {copy.confirmPassword}
                <input type="password" value={registerForm.confirmPassword} onChange={(event) => setRegisterForm({ ...registerForm, confirmPassword: event.target.value })} />
              </label>
              <button type="submit" className="button primary">{copy.register}</button>
            </form>
          ) : (
            <form className="card-form" onSubmit={handleLogin}>
              <label>
                {copy.email}
                <input type="email" value={loginForm.email} onChange={(event) => setLoginForm({ ...loginForm, email: event.target.value })} />
              </label>
              <label>
                {copy.password}
                <input type="password" value={loginForm.password} onChange={(event) => setLoginForm({ ...loginForm, password: event.target.value })} />
              </label>
              <button type="submit" className="button primary">{copy.login}</button>
            </form>
          )}
        </section>
      ) : (
        <section className="card-stack">
          <div className="account-header">
            <div>
              <p className="eyebrow">{copy.welcome}</p>
              <h2>{household.householdName}</h2>
            </div>
            <button type="button" className="button secondary" onClick={handleSignOut}>{copy.signOut}</button>
          </div>

          {notice ? <p className="notice">{notice}</p> : null}

          <div className="rsvp-panel">
            <div className="panel-header">
              <h3>{copy.eventCeremony}</h3>
              <select value={household.rsvp.ceremony} onChange={(event) => updateEventAttendance('ceremony', event.target.value as HouseholdAccount['rsvp'][string])}>
                <option value="NOT_ANSWERED">{copy.notAnswered}</option>
                <option value="ATTENDING">{copy.attending}</option>
                <option value="NOT_ATTENDING">{copy.notAttending}</option>
              </select>
            </div>
            <div className="panel-header">
              <h3>{copy.eventDinner}</h3>
              <select value={household.rsvp.dinner} onChange={(event) => updateEventAttendance('dinner', event.target.value as HouseholdAccount['rsvp'][string])}>
                <option value="NOT_ANSWERED">{copy.notAnswered}</option>
                <option value="ATTENDING">{copy.attending}</option>
                <option value="NOT_ATTENDING">{copy.notAttending}</option>
              </select>
            </div>
            <div className="panel-header">
              <h3>{copy.eventBrunch}</h3>
              <select value={household.rsvp.brunch} onChange={(event) => updateEventAttendance('brunch', event.target.value as HouseholdAccount['rsvp'][string])}>
                <option value="NOT_ANSWERED">{copy.notAnswered}</option>
                <option value="ATTENDING">{copy.attending}</option>
                <option value="NOT_ATTENDING">{copy.notAttending}</option>
              </select>
            </div>
          </div>

          <div className="guest-panel">
            <div className="section-header">
              <h3>{copy.guestList}</h3>
              <div className="inline-actions">
                <button type="button" className="button secondary" onClick={() => addGuest('adults')}>{copy.addAdult}</button>
                <button type="button" className="button secondary" onClick={() => addGuest('children')}>{copy.addChild}</button>
                <button type="button" className="button primary" onClick={saveHouseholdChanges}>Save household changes</button>
              </div>
            </div>

            <div className="guest-list">
              {household.adults.length === 0 && household.children.length === 0 ? <p>{copy.emptyState}</p> : null}

              {household.adults.map((guest) => (
                <div className="guest-card" key={guest.id}>
                  <div className="field-grid two-up">
                    <label>
                      {copy.firstName}
                      <input value={guest.firstName} onChange={(event) => updateGuest(guest.id, 'adults', 'firstName', event.target.value)} />
                    </label>
                    <label>
                      {copy.lastName}
                      <input value={guest.lastName} onChange={(event) => updateGuest(guest.id, 'adults', 'lastName', event.target.value)} />
                    </label>
                  </div>
                  <div className="field-grid two-up">
                    <label>
                      {copy.status}
                      <select value={guest.attendance} onChange={(event) => updateAttendance(guest.id, 'adults', 'attendance', event.target.value as HouseholdGuest['attendance'])}>
                        <option value="NOT_ANSWERED">{copy.notAnswered}</option>
                        <option value="ATTENDING">{copy.attending}</option>
                        <option value="NOT_ATTENDING">{copy.notAttending}</option>
                      </select>
                    </label>
                    <label>
                      {copy.dietary}
                      <input value={guest.dietaryRestrictions ?? ''} onChange={(event) => updateGuest(guest.id, 'adults', 'dietaryRestrictions', event.target.value)} />
                    </label>
                  </div>
                  <div className="field-grid two-up">
                    <label>
                      {copy.allergies}
                      <input value={guest.allergies ?? ''} onChange={(event) => updateGuest(guest.id, 'adults', 'allergies', event.target.value)} />
                    </label>
                    <label>
                      {copy.accessibility}
                      <input value={guest.accessibilityRequirements ?? ''} onChange={(event) => updateGuest(guest.id, 'adults', 'accessibilityRequirements', event.target.value)} />
                    </label>
                  </div>
                  <label>
                    {copy.note}
                    <input value={guest.note ?? ''} onChange={(event) => updateGuest(guest.id, 'adults', 'note', event.target.value)} />
                  </label>
                </div>
              ))}

              {household.children.map((guest) => (
                <div className="guest-card" key={guest.id}>
                  <div className="field-grid two-up">
                    <label>
                      {copy.firstName}
                      <input value={guest.firstName} onChange={(event) => updateGuest(guest.id, 'children', 'firstName', event.target.value)} />
                    </label>
                    <label>
                      {copy.lastName}
                      <input value={guest.lastName} onChange={(event) => updateGuest(guest.id, 'children', 'lastName', event.target.value)} />
                    </label>
                  </div>
                  <div className="field-grid two-up">
                    <label>
                      {copy.status}
                      <select value={guest.attendance} onChange={(event) => updateAttendance(guest.id, 'children', 'attendance', event.target.value as HouseholdGuest['attendance'])}>
                        <option value="NOT_ANSWERED">{copy.notAnswered}</option>
                        <option value="ATTENDING">{copy.attending}</option>
                        <option value="NOT_ATTENDING">{copy.notAttending}</option>
                      </select>
                    </label>
                    <label>
                      {copy.dietary}
                      <input value={guest.dietaryRestrictions ?? ''} onChange={(event) => updateGuest(guest.id, 'children', 'dietaryRestrictions', event.target.value)} />
                    </label>
                  </div>
                  <div className="field-grid two-up">
                    <label>
                      {copy.allergies}
                      <input value={guest.allergies ?? ''} onChange={(event) => updateGuest(guest.id, 'children', 'allergies', event.target.value)} />
                    </label>
                    <label>
                      {copy.accessibility}
                      <input value={guest.accessibilityRequirements ?? ''} onChange={(event) => updateGuest(guest.id, 'children', 'accessibilityRequirements', event.target.value)} />
                    </label>
                  </div>
                  <label>
                    {copy.note}
                    <input value={guest.note ?? ''} onChange={(event) => updateGuest(guest.id, 'children', 'note', event.target.value)} />
                  </label>
                </div>
              ))}
            </div>
          </div>
        </section>
      )}
    </main>
  );
}

function GalleryPage({ locale }: { locale: Locale }) {
  const copy = content[locale];
  const galleryImages = getEditorialGalleryImages(locale);

  return (
    <main className="page-shell">
      <h1>{copy.sections.gallery}</h1>
      <div className="gallery-grid">
        {(galleryImages.length > 0 ? galleryImages : []).map((image, index) => (
          <div className="photo-card" key={`${image}-${index}`}>
            <img src={image} alt={`${copy.sections.gallery} ${index + 1}`} style={{ width: '100%', height: '220px', objectFit: 'cover', borderRadius: '12px' }} />
          </div>
        ))}
      </div>
    </main>
  );
}

export default function App() {
  const [locale, setLocale] = useState<Locale>(() => {
    const stored = localStorage.getItem('wedding-locale') as Locale | null;
    return stored === 'fr' || stored === 'en' ? stored : 'en';
  });

  useEffect(() => {
    document.documentElement.lang = locale;
    localStorage.setItem('wedding-locale', locale);
  }, [locale]);

  const navItems = useMemo(() => [
    { to: '/', label: content[locale].nav.home },
    { to: '/rsvp', label: content[locale].nav.rsvp },
    { to: '/gallery', label: content[locale].nav.gallery },
  ], [locale]);

  return (
    <>
      <nav className="topbar" aria-label="Main navigation">
        <div className="brand" aria-label="Brand">J&F</div>

        <div className="topbar-links">
          {navItems.map(({ to, label }) => (
            <Link key={to} to={to}>{label}</Link>
          ))}
        </div>

        <div className="language-switch" aria-label="Language selector">
          <button type="button" className={locale === 'fr' ? 'active' : ''} onClick={() => setLocale('fr')}>FR</button>
          <button type="button" className={locale === 'en' ? 'active' : ''} onClick={() => setLocale('en')}>EN</button>
        </div>
      </nav>
      <Routes>
        <Route path="/" element={<HomePage locale={locale} />} />
        <Route path="/rsvp" element={<RSVPPage locale={locale} />} />
        <Route path="/gallery" element={<GalleryPage locale={locale} />} />
      </Routes>
    </>
  );
}
