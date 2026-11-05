import { Component, OnInit, signal } from '@angular/core';

@Component({
  selector: 'app-root',
  standalone: true,
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  protected readonly title = signal('EthioFleet');
  protected readonly apiStatus = signal('Checking API health...');

  ngOnInit(): void {
    fetch('http://localhost:5062/api/v1/health')
      .then(async (response) => {
        if (!response.ok) {
          throw new Error(`API returned status ${response.status}`);
        }

        const payload = await response.json();
        this.apiStatus.set(`API status: ${payload.status} — ${payload.service}`);
      })
      .catch(() => {
        this.apiStatus.set('API unavailable. Start the .NET API to see live health data.');
      });
  }
}
