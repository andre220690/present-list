import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api, ApiError, PublicGift } from '../api/client';
import { GiftCard } from '../components/GiftCard';
import { GiftModal } from '../components/GiftModal';
import { PhotoBackdrop } from '../components/PhotoBackdrop';

export function GiftsPage() {
  const [gifts, setGifts] = useState<PublicGift[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedGift, setSelectedGift] = useState<PublicGift | null>(null);

  async function loadGifts() {
    setError(null);
    try {
      setGifts(await api.getGifts());
    } catch (err) {
      setError((err as ApiError).message);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadGifts();
  }, []);

  return (
    <main className="giftsPage">
      <PhotoBackdrop />
      <section className="pageContent">
        <header className="giftsHeader">
          <Link className="secondaryButton" to="/">К приглашению</Link>
          <Link className="secondaryButton" to="/admin/login">Вход администратора</Link>
          <div className="headlineBlock">
            <h1>Список желаемых подарков</h1>
            <p>Если желаете, можете подарить свой вариант подарка. Полина будет рада любому подарку.</p>
          </div>
        </header>

        {loading && <p className="statePanel">Загружаем список подарков...</p>}
        {error && <p className="statePanel errorText">{error}</p>}
        {!loading && !error && gifts.length === 0 && (
          <p className="statePanel">Список пока пуст. Скоро здесь появятся идеи подарков.</p>
        )}
        <div className="giftGrid">
          {gifts.map((gift) => (
            <GiftCard key={gift.id} gift={gift} onOpen={setSelectedGift} />
          ))}
        </div>
      </section>

      {selectedGift && (
        <GiftModal
          gift={selectedGift}
          onClose={() => setSelectedGift(null)}
          onChanged={loadGifts}
        />
      )}
    </main>
  );
}
