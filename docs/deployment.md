# Mercadotnet API — Render.com Deployment

## Overview

This guide covers deploying the Mercadotnet API on [Render.com](https://render.com) using their Docker runtime.

## Prerequisites

- A [Render.com](https://render.com) account
- The project pushed to a Git repository (GitHub, GitLab, or Bitbucket)

## Resources to Create

### 1. PostgreSQL Database

1. Go to **Dashboard → New → PostgreSQL**
2. Plan: **Starter** (free) or **Standard** ($7/mo)
3. Database name: `mercadotnet`
4. Note the **Internal Database URL** (for the web service) and **External URL** (for local CLI)

### 2. Redis (Optional, for caching)

You'll need a Redis provider. Options:
- [Redis Cloud](https://redis.com/try-free/) — Free 30MB plan
- [Upstash](https://upstash.com) — Free tier with TLS
- Add the connection string to environment variables below

### 3. Web Service (Docker)

1. **Dashboard → New → Web Service**
2. Connect your Git repository
3. **Name:** `mercadotnet-api`
4. **Runtime:** `Docker`
5. **Region:** Choose closest to your users
6. **Branch:** `main`
7. **Health Check Path:** `/health`
8. **Plan:** Starter ($7/mo) or higher

#### Environment Variables

Set these under **Environment** in the Render dashboard:

| Variable | Value |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_URLS` | `http://+:8080` |
| `JWT__SECRET` | Generate with: `openssl rand -base64 32` |
| `JWT__ISSUER` | `mercadotnet-api` |
| `JWT__AUDIENCE` | `mercadotnet-clients` |
| `JWT__ACCESSTOKENLIFETIMEMINUTES` | `15` |
| `JWT__REFRESHTOKENLIFETIMEDAYS` | `7` |
| `DATABASE__CONNECTIONSTRING` | `Host=<internal-db-host>;Port=5432;Database=mercadotnet;Username=<user>;Password=<password>` |
| `REDIS__CONNECTIONSTRING` | `localhost:6379` — or your Redis provider URL |
| `EMAIL__PROVIDER` | `smtp` |
| `EMAIL__SMTP__HOST` | Your SMTP host (e.g., `smtp.sendgrid.net`) |
| `EMAIL__SMTP__PORT` | `587` |
| `EMAIL__SMTP__USETLS` | `true` |
| `EMAIL__SMTP__USERNAME` | SMTP username |
| `EMAIL__SMTP__PASSWORD` | SMTP password |
| `EMAIL__SMTP__FROM` | `noreply@yourdomain.com` |
| `EMAIL__SMTP__FROMNAME` | `Mercadotnet` |
| `STORAGE__PROVIDER` | `s3` (or `cloudinary`, `local`) |
| `STORAGE__S3__BUCKETNAME` | (if using S3) |
| `STORAGE__S3__REGION` | (if using S3) |
| `STORAGE__S3__ACCESSKEY` | (if using S3) |
| `STORAGE__S3__SECRETKEY` | (if using S3) |
| `CORS__ALLOWEDORIGINS__0` | `https://app.yourdomain.com` |
| `SENTRY__DSN` | Optional — error tracking |

> **Important for Starters:** Render's free/starter plans sleep after inactivity. The first request after sleep will have a 30-60s cold start.

## Managed PostgreSQL

Render provides a managed PostgreSQL instance with automated backups. Use the **Internal Database URL** in the web service environment — this keeps traffic within Render's network (no public internet).

## Production Checklist

- [ ] Health check path set to `/health`
- [ ] `JWT__SECRET` is a strong, unique value
- [ ] PostgreSQL connection uses **Internal** URL (not External)
- [ ] `ASPNETCORE_ENVIRONMENT` is `Production` (hides Swagger)
- [ ] CORS origins scoped to your frontend domain(s)
- [ ] Sentry DSN configured for 5xx error tracking
- [ ] Storage provider configured (S3 or Cloudinary for production)
- [ ] Email provider configured (Resend, ZeptoMail, or SendGrid SMTP)

## Updating

Push to your Git branch — Render auto-deploys. Alternatively use the **Manual Deploy** button in the dashboard.

## Running CLI Commands

```bash
# Via Render Shell:
# 1. Go to Dashboard → mercadotnet-api → Shell
# 2. Run:
dotnet API.dll cli
dotnet API.dll cli seed:admin
```
