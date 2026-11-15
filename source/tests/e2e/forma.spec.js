import { readFile } from "node:fs/promises";
import { test, expect } from "@playwright/test";

async function showApp(page) {
  await page.clock.setFixedTime(new Date("2026-10-30T12:00:00Z"));
  await page.goto("./");
  await expect(page.getByTestId("new-habit")).toBeVisible();
}

async function addHabit(page, name, quantity) {
  await page.getByTestId("new-habit").click();
  await page.getByTestId("habit-name").fill(name);
  if (quantity) {
    await page.getByTestId("habit-kind").selectOption("Quantity");
    await page.getByTestId("habit-target").fill(String(quantity));
    await page.getByTestId("habit-unit").fill("pages");
  }
  await page.getByTestId("save-habit").click();
  await expect(page.getByTestId("habit-name")).not.toBeVisible();
  await expect(page.getByText(name, { exact: true }).first()).toBeVisible();
}

async function exportBackup(page) {
  const [download] = await Promise.all([
    page.waitForEvent("download"),
    page.getByTestId("export-backup").click(),
  ]);
  const filePath = await download.path();
  if (!filePath) throw new Error("The browser reported no backup file.");
  return { name: download.suggestedFilename(), buffer: await readFile(filePath) };
}

test("logs quantity progress, survives reload, and opens review views", async ({ page }) => {
  await showApp(page);
  await addHabit(page, "Read two pages", 2);
  await page.getByTestId("increment-progress").click();
  await expect(page.getByText("1 / 2 pages", { exact: true })).toBeVisible();
  await page.getByTestId("increment-progress").click();
  await expect(page.getByText("2 / 2 pages", { exact: true })).toBeVisible();
  await page.reload();
  await expect(page.getByText("Read two pages", { exact: true })).toBeVisible();

  await page.getByTestId("nav-history").click();
  await expect(page.getByRole("heading", { name: /look how far/i })).toBeVisible();
  await expect(page.getByText("2 / 2 pages", { exact: true })).toBeVisible();
  await page.getByTestId("nav-insights").click();
  await expect(page.getByRole("heading", { name: /a clearer picture/i })).toBeVisible();

  await page.getByTestId("nav-settings").click();
  const downloaded = await exportBackup(page);
  const data = downloaded.buffer.toString("utf8");
  expect(data).toContain("Read two pages");
  const parsed = JSON.parse(data);
  expect(parsed.version).toBe(1);
  expect(Object.values(parsed.state.logs)).toEqual([{ "2026-10-30": 2 }]);
});

test("round trips a backup and does not replace data for invalid JSON", async ({ page }) => {
  await showApp(page);
  await addHabit(page, "Keep this habit");
  await page.getByTestId("toggle-complete").click();
  await expect(page.getByTestId("toggle-complete")).toHaveAttribute("aria-pressed", "true");
  await page.getByTestId("nav-settings").click();
  const backup = await exportBackup(page);

  await page.getByTestId("import-backup").setInputFiles({
    name: "invalid.json",
    mimeType: "application/json",
    buffer: Buffer.from("{invalid"),
  });
  await expect(page.getByRole("alert")).toContainText("valid Forma JSON backup");
  await expect(page.getByTestId("confirm-import")).not.toBeVisible();
  await page.getByTestId("nav-today").click();
  await expect(page.getByText("Keep this habit", { exact: true })).toBeVisible();
  await addHabit(page, "Temporary habit");

  await page.getByTestId("nav-settings").click();
  await page.getByTestId("import-backup").setInputFiles({
    ...backup,
    mimeType: "application/json",
  });
  await page.getByTestId("confirm-import").click();
  await expect(page.getByTestId("confirm-import")).not.toBeVisible();
  await page.getByTestId("nav-today").click();
  await expect(page.getByText("Keep this habit", { exact: true })).toBeVisible();
  await expect(page.getByText("Temporary habit", { exact: true })).toHaveCount(0);
  await expect(page.getByTestId("toggle-complete")).toHaveAttribute("aria-pressed", "true");
});

test("edits and deletes a habit from the management view", async ({ page }) => {
  await showApp(page);
  await addHabit(page, "Original walk");
  await page.getByTestId("nav-habits").click();
  await page.getByTestId("edit-habit").click();
  await page.getByTestId("habit-name").fill("A shorter walk");
  await page.getByTestId("save-habit").click();
  await expect(page.getByText("A shorter walk", { exact: true })).toBeVisible();
  await expect(page.getByText("Original walk", { exact: true })).toHaveCount(0);

  await page.getByTestId("delete-habit").click();
  await page.getByRole("button", { name: "Cancel", exact: true }).click();
  await expect(page.getByTestId("confirm-delete")).not.toBeVisible();
  await expect(page.getByText("A shorter walk", { exact: true })).toBeVisible();
  await page.getByTestId("delete-habit").click();
  await page.getByTestId("confirm-delete").click();
  await expect(page.getByText("A shorter walk", { exact: true })).toHaveCount(0);
  await page.getByTestId("nav-today").click();
  await expect(page.getByText("A shorter walk", { exact: true })).toHaveCount(0);
});

test("reports a storage write failure without adding an unsaved habit", async ({ page }) => {
  await page.addInitScript(() => {
    const write = Storage.prototype.setItem;
    Storage.prototype.setItem = function (key, value) {
      if (key === "forma.blazor.v1") throw new DOMException("Test quota exhaustion", "QuotaExceededError");
      return write.call(this, key, value);
    };
  });

  await showApp(page);
  await page.getByTestId("new-habit").click();
  await page.getByTestId("habit-name").fill("Not saved");
  await page.getByTestId("save-habit").click();
  await expect(page.getByRole("alert")).toContainText("Changes weren’t saved");
  await expect(page.getByTestId("habit-name")).toHaveValue("Not saved");
  await page.getByRole("button", { name: "Cancel", exact: true }).click();
  await expect(page.getByRole("heading", { name: "Not saved", exact: true })).toHaveCount(0);
  await page.reload();
  await expect(page.getByTestId("new-habit")).toBeVisible();
  await expect(page.getByRole("heading", { name: "Not saved", exact: true })).toHaveCount(0);
});

test("mobile keeps navigation and the editor within the viewport @mobile", async ({ page }, info) => {
  if (!info.project.name.endsWith("-emulated"))
    await page.setViewportSize({ width: 390, height: 844 });
  await showApp(page);
  await expect(page.getByTestId("nav-today")).toBeVisible();
  await expect.poll(() => page.locator("html").evaluate(
    (element) => element.scrollWidth <= element.clientWidth
  )).toBe(true);
  await page.getByTestId("new-habit").click();
  await expect(page.getByTestId("habit-name")).toBeFocused();
  await expect.poll(() => page.locator("html").evaluate(
    (element) => element.scrollWidth <= element.clientWidth
  )).toBe(true);
  await page.getByTestId("habit-name").fill("Read on a small screen");
  await page.getByTestId("save-habit").click();
  await expect(page.getByRole("heading", { name: "Read on a small screen" })).toBeVisible();
  await page.getByTestId("toggle-complete").click();
  await expect(page.getByTestId("toggle-complete")).toHaveAttribute("aria-pressed", "true");
  await page.getByTestId("nav-history").click();
  await expect(page.getByRole("heading", { name: /look how far/i })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Read on a small screen" })).toBeVisible();
});
