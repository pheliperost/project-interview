let lockCount = 0;

export function lockPageScroll() {
  lockCount += 1;
  if (lockCount === 1) {
    document.documentElement.classList.add('scroll-locked');
    document.body.classList.add('scroll-locked');
    document.getElementById('root')?.setAttribute('inert', '');
  }
}

export function unlockPageScroll() {
  lockCount = Math.max(0, lockCount - 1);
  if (lockCount === 0) {
    document.documentElement.classList.remove('scroll-locked');
    document.body.classList.remove('scroll-locked');
    document.getElementById('root')?.removeAttribute('inert');
  }
}

export function resetPageScrollLock() {
  lockCount = 0;
  document.documentElement.classList.remove('scroll-locked');
  document.body.classList.remove('scroll-locked');
  document.getElementById('root')?.removeAttribute('inert');
}
