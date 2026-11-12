import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
    })
      .compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render title', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('EthioFleet');
  });

  it('should display readiness from the API health endpoint', async () => {
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(JSON.stringify({ status: 'healthy', checks: { postgresql: 'healthy' } }), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    );

    const fixture = TestBed.createComponent(App);
    try {
      await vi.waitFor(() => {
        fixture.detectChanges();
        expect(fixture.nativeElement.textContent).toContain('API status: healthy');
      });
      expect(fetchSpy).toHaveBeenCalledWith('http://localhost:5062/health/ready');
    } finally {
      fetchSpy.mockRestore();
    }
  });
});
