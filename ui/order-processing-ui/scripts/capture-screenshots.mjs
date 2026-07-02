import { mkdir } from 'node:fs/promises';
import puppeteer from 'puppeteer-core';

const chromePath = '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome';
const outputDir = '../../docs/images';

await mkdir(outputDir, { recursive: true });

const browser = await puppeteer.launch({
  executablePath: chromePath,
  headless: 'new',
  args: ['--no-first-run', '--disable-gpu']
});

const page = await browser.newPage();
await page.setViewport({ width: 1440, height: 1100, deviceScaleFactor: 1 });
await page.goto('http://localhost:5175/', { waitUntil: 'networkidle0' });
await page.screenshot({ path: `${outputDir}/order-console-dashboard.png`, fullPage: true });

await page.locator('button[aria-label="New order"]').click();
await page.waitForSelector('[data-testid="create-order-dialog"]');
await new Promise((resolve) => setTimeout(resolve, 500));
await page.screenshot({ path: `${outputDir}/order-console-create-order.png`, fullPage: true });

await browser.close();
