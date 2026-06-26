import { test, expect } from '@playwright/test';

test.beforeEach(async ({ request }) => {
  await request.delete('/api/orders');
});

test('clerk creates an order - empty order has total of zero', async ({ page }) => {
  await page.goto('/');
  await page.click('[data-testid="create-order-btn"]');

  const card = page.locator('[data-testid="order-card"]').first();
  await expect(card).toBeVisible();
  await expect(card.locator('[data-testid="order-total"]')).toContainText('0.00');
});

test('clerk adds a product - order total reflects discounted price', async ({ page }) => {
  await page.goto('/');
  await page.click('[data-testid="create-order-btn"]');

  const card = page.locator('[data-testid="order-card"]').first();
  await expect(card).toBeVisible();
  await card.locator('[data-testid="add-product-btn"]').click();

  const form = card.locator('[data-testid="product-form"]');
  await expect(form).toBeVisible();
  await form.locator('[name="name"]').fill('Glass Vase');
  await form.locator('[name="color"]').selectOption('Red');
  await form.locator('[name="size"]').selectOption('Medium');
  await form.locator('[name="price"]').fill('25.00');
  await form.locator('[name="discount"]').fill('0.1');
  await form.locator('[name="material"]').selectOption('Glass');
  await form.locator('[name="weightKg"]').fill('0.5');
  await form.locator('[name="fragile"]').check();
  await form.locator('[name="packaging"]').selectOption('Boxed');
  await form.locator('[name="lengthCm"]').fill('10');
  await form.locator('[name="widthCm"]').fill('10');
  await form.locator('[name="heightCm"]').fill('20');
  await form.locator('[data-testid="submit-product-btn"]').click();

  await expect(card.locator('[data-testid="order-total"]')).toContainText('22.50');
});

test('clerk adds two products - order total is sum of discounted prices', async ({ page }) => {
  await page.goto('/');
  await page.click('[data-testid="create-order-btn"]');

  const card = page.locator('[data-testid="order-card"]').first();
  await expect(card).toBeVisible();

  async function addProduct(name: string, price: string, discount: string) {
    await card.locator('[data-testid="add-product-btn"]').click();
    const form = card.locator('[data-testid="product-form"]');
    await expect(form).toBeVisible();
    await form.locator('[name="name"]').fill(name);
    await form.locator('[name="price"]').fill(price);
    await form.locator('[name="discount"]').fill(discount);
    await form.locator('[name="weightKg"]').fill('1');
    await form.locator('[name="lengthCm"]').fill('10');
    await form.locator('[name="widthCm"]').fill('10');
    await form.locator('[name="heightCm"]').fill('10');
    await form.locator('[data-testid="submit-product-btn"]').click();
    await expect(form).not.toBeVisible();
  }

  await addProduct('Widget A', '100', '0.1');
  await addProduct('Widget B', '50', '0.2');

  await expect(card.locator('[data-testid="order-total"]')).toContainText('130.00');
});

test('orders persist - reload shows existing orders', async ({ page }) => {
  await page.goto('/');
  await page.click('[data-testid="create-order-btn"]');
  await expect(page.locator('[data-testid="order-card"]')).toHaveCount(1);

  await page.reload();
  await expect(page.locator('[data-testid="order-card"]')).toHaveCount(1);
});

test('clerk confirms a pending order - status changes to Confirmed', async ({ page }) => {
  await page.goto('/');
  await page.click('[data-testid="create-order-btn"]');

  const card = page.locator('[data-testid="order-card"]').first();
  await expect(card).toBeVisible();
  await expect(card.locator('[data-testid="order-status"]')).toContainText('Pending');

  await card.locator('[data-testid="confirm-btn"]').click();

  await expect(card.locator('[data-testid="order-status"]')).toContainText('Confirmed');
  await expect(card.locator('[data-testid="confirm-btn"]')).not.toBeVisible();
  await expect(card.locator('[data-testid="add-product-btn"]')).not.toBeVisible();
});
