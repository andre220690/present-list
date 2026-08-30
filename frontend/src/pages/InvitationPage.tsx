import type { CSSProperties } from 'react';
import { Link } from 'react-router-dom';
import invitation from '../assets/invitation.png';

export function InvitationPage() {
  return (
    <main
      className="invitationPage"
      style={{ '--invitation-bg': `url(${invitation})` } as CSSProperties}
      aria-label="Приглашение на день рождения Полины в Мурмилэнд"
    >
      <div className="invitationShell">
        <Link className="giftCta" to="/gifts">
          Что мне можно подарить
        </Link>
      </div>
    </main>
  );
}
