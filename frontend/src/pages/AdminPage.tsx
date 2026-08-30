import { ClipboardEvent, FormEvent, useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { AdminGift, api, ApiError } from '../api/client';

export function AdminPage() {
  const navigate = useNavigate();
  const [gifts, setGifts] = useState<AdminGift[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [pastedImage, setPastedImage] = useState<File | null>(null);
  const [pastedImageUrl, setPastedImageUrl] = useState<string | null>(null);

  async function load() {
    try {
      await api.session();
      setGifts(await api.adminGifts());
      setError(null);
    } catch (err) {
      const apiError = err as ApiError;
      if (apiError.message === 'Нужно войти как администратор.') {
        navigate('/admin/login');
        return;
      }
      setError(apiError.message);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    load();
  }, []);

  useEffect(() => {
    return () => {
      if (pastedImageUrl) {
        URL.revokeObjectURL(pastedImageUrl);
      }
    };
  }, [pastedImageUrl]);

  async function addGift(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formElement = event.currentTarget;
    const form = new FormData(formElement);
    const manualImage = form.get('image');

    if (!(manualImage instanceof File && manualImage.size > 0)) {
      if (!pastedImage) {
        setError('Добавьте изображение вручную или вставьте фотографию из буфера обмена.');
        return;
      }

      form.set('image', pastedImage, pastedImage.name);
    }

    setBusy(true);
    setError(null);
    setSuccess(null);
    try {
      const created = await api.addGift(form);
      setGifts((current) => [created, ...current]);
      formElement.reset();
      clearPastedImage();
      setSuccess('Подарок добавлен.');
    } catch (err) {
      setError((err as ApiError).message);
    } finally {
      setBusy(false);
    }
  }

  async function deleteGift(id: string) {
    if (!window.confirm('Удалить этот подарок?')) return;
    setBusy(true);
    try {
      await api.deleteGift(id);
      setGifts((current) => current.filter((gift) => gift.id !== id));
    } catch (err) {
      setError((err as ApiError).message);
    } finally {
      setBusy(false);
    }
  }

  async function clearReservation(id: string) {
    setBusy(true);
    try {
      await api.clearReservation(id);
      await load();
    } catch (err) {
      setError((err as ApiError).message);
    } finally {
      setBusy(false);
    }
  }

  async function logout() {
    await api.logout();
    navigate('/admin/login');
  }

  function handlePaste(event: ClipboardEvent<HTMLFormElement | HTMLDivElement>) {
    const file = getClipboardImage(event.clipboardData);
    if (!file) {
      return;
    }

    event.preventDefault();
    setPastedImage(file);
    setPastedImageUrl((currentUrl) => {
      if (currentUrl) {
        URL.revokeObjectURL(currentUrl);
      }
      return URL.createObjectURL(file);
    });
    setError(null);
    setSuccess('Изображение вставлено из буфера обмена.');
  }

  function clearPastedImage() {
    setPastedImage(null);
    setPastedImageUrl((currentUrl) => {
      if (currentUrl) {
        URL.revokeObjectURL(currentUrl);
      }
      return null;
    });
  }

  return (
    <main className="adminPage">
      <header className="adminHeader">
        <h1>Панель администратора</h1>
        <div className="adminActions">
          <Link className="secondaryButton" to="/gifts">Публичный список</Link>
          <button className="secondaryButton" type="button" onClick={logout}>Выйти</button>
        </div>
      </header>

      {loading && <p className="statePanel">Загружаем панель...</p>}
      {error && <p className="statePanel errorText">{error}</p>}
      {success && <p className="statePanel successText">{success}</p>}

      <form className="adminForm" onSubmit={addGift} onPaste={handlePaste}>
        <h2>Добавить подарок</h2>
        <label className="fieldLabel">
          Изображение
          <input
            name="image"
            type="file"
            accept="image/png,image/jpeg,image/webp"
            onChange={(event) => {
              if (event.currentTarget.files?.length) {
                clearPastedImage();
              }
            }}
          />
        </label>
        <div className="clipboardPasteZone" tabIndex={0} onPaste={handlePaste}>
          <span>Вставьте фотографию из буфера обмена: Ctrl+V</span>
          {pastedImageUrl && (
            <div className="pastedImagePreview">
              <img src={pastedImageUrl} alt="Изображение из буфера обмена" />
              <button className="secondaryButton" type="button" onClick={clearPastedImage}>
                Убрать
              </button>
            </div>
          )}
        </div>
        <label className="fieldLabel">
          Ссылка на товар
          <input name="productUrl" type="url" required />
        </label>
        <label className="fieldLabel">Название<input name="name" minLength={2} maxLength={150} required /></label>
        <label className="fieldLabel">Описание<textarea name="description" maxLength={2000} rows={4} /></label>
        <button className="primaryButton" type="submit" disabled={busy}>Добавить</button>
      </form>

      <section className="adminList">
        <h2>Подарки</h2>
        {gifts.length === 0 && !loading && <p className="statePanel">Подарков пока нет.</p>}
        {gifts.map((gift) => (
          <article className="adminGift" key={gift.id}>
            <img src={gift.imageUrl} alt={gift.name} />
            <div>
              <h3>{gift.name}</h3>
              <p>{gift.description || 'Без описания'}</p>
              <a href={gift.productUrl} target="_blank" rel="noopener noreferrer">Открыть ссылку</a>
              {gift.isReserved ? (
                <div className="reservationInfo">
                  <p className="reservedText">Забронирован</p>
                  <p>Имя гостя: <strong>{gift.reservedByName || 'не указано'}</strong></p>
                </div>
              ) : (
                <p className="availableText">Свободен</p>
              )}
            </div>
            <div className="adminGiftActions">
              {gift.isReserved && (
                <button className="secondaryButton" type="button" onClick={() => clearReservation(gift.id)} disabled={busy}>
                  Снять бронь
                </button>
              )}
              <button className="dangerButton" type="button" onClick={() => deleteGift(gift.id)} disabled={busy}>
                Удалить
              </button>
            </div>
          </article>
        ))}
      </section>
    </main>
  );
}

function getClipboardImage(data: DataTransfer) {
  const fileFromList = Array.from(data.files).find(isAllowedImageFile);
  if (fileFromList) {
    return normalizeClipboardFile(fileFromList);
  }

  for (const item of Array.from(data.items)) {
    if (item.kind !== 'file') {
      continue;
    }

    const file = item.getAsFile();
    if (file && isAllowedImageFile(file)) {
      return normalizeClipboardFile(file);
    }
  }

  return null;
}

function isAllowedImageFile(file: File) {
  return ['image/jpeg', 'image/png', 'image/webp'].includes(file.type);
}

function normalizeClipboardFile(file: File) {
  if (file.name) {
    return file;
  }

  const extension = file.type === 'image/webp' ? 'webp' : file.type === 'image/jpeg' ? 'jpg' : 'png';
  return new File([file], `clipboard-image.${extension}`, { type: file.type });
}
